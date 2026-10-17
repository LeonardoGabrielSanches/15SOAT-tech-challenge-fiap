using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Application.CatalogoEstoque.UseCases;
using Oficina.Application.Common;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class TempoMedioDeExecucaoTests : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";
    private const string RotaPublica = "/api/publico/orcamentos";

    private readonly ApiFactory _origem;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<string> _tokens = [];
    private DateTimeOffset _agora = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    public TempoMedioDeExecucaoTests(PostgresFixture postgres)
    {
        _origem = new ApiFactory(postgres);
        var notificador = new Mock<INotificador>();
        notificador
            .Setup(n => n.EnviarOrcamento(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, decimal, string, CancellationToken>((_, _, _, k, _) => _tokens.Add(k))
            .Returns(Task.CompletedTask);
        _factory = _origem.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<INotificador>();
            s.AddSingleton(notificador.Object);
            var relogio = new Mock<TimeProvider>();
            relogio.Setup(r => r.GetUtcNow()).Returns(() => _agora);
            s.RemoveAll<TimeProvider>();
            s.AddSingleton(relogio.Object);
        }));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _origem.DisposeAsync();
    }

    private HttpClient Como(string papel)
    {
        var http = _factory.CreateClient();
        http.DefaultRequestHeaders.Add(ApiFactory.CabecalhoPapel, papel);
        return http;
    }

    private static string CpfNovo()
    {
        var digitos = Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(10)).ToList();
        if (digitos.Distinct().Count() == 1) digitos[0] = (digitos[0] + 1) % 10;
        digitos.Add(Digito(digitos, 10));
        digitos.Add(Digito(digitos, 11));
        return string.Concat(digitos);
    }

    private static int Digito(List<int> digitos, int pesoInicial)
    {
        var resto = digitos.Select((d, i) => d * (pesoInicial - i)).Sum() % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static string PlacaNova() =>
        new string(Enumerable.Range(0, 3).Select(_ => (char)('A' + Random.Shared.Next(26))).ToArray()) + Random.Shared.Next(1000, 10000);

    private async Task<(Guid OsId, Guid ServicoId)> OsAprovada()
    {
        var servico = (await (await Como("gerente").PostAsJsonAsync("/api/administrativo/servicos",
            new { nome = $"Servico {Guid.NewGuid():N}", descricao = (string?)null, preco = 150m })).Content.ReadFromJsonAsync<ServicoDto>())!;
        var atendente = Como("atendente");
        var cpf = CpfNovo();
        var cliente = (await (await atendente.PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = cpf, email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null })).Content.ReadFromJsonAsync<ClienteDto>())!;
        var veiculo = (await (await atendente.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id })).Content.ReadFromJsonAsync<VeiculoDto>())!;
        var os = (await (await atendente.PostAsJsonAsync(Rota,
            new { clienteId = cliente.Id, veiculoId = veiculo.Id, servicoIds = new[] { servico.Id } })).Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;

        var mecanico = Como("mecanico");
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{os.Id}/diagnostico/iniciar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{os.Id}/diagnostico/concluir", null)).StatusCode);
        var token = _tokens[^1];
        Assert.Equal(HttpStatusCode.NoContent, (await _factory.CreateClient().PostAsync($"{RotaPublica}/{token}/aprovar", null)).StatusCode);
        return (os.Id, servico.Id);
    }

    private async Task Executar(Guid osId, TimeSpan duracao, bool entregar)
    {
        var mecanico = Como("mecanico");
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{osId}/execucao/iniciar", null)).StatusCode);
        _agora += duracao;
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{osId}/execucao/finalizar", null)).StatusCode);
        if (entregar)
        {
            Assert.Equal(HttpStatusCode.NoContent, (await Como("atendente").PostAsync($"{Rota}/{osId}/entrega", null)).StatusCode);
        }
    }

    [Fact]
    public async Task Media_de_duas_os_executadas_em_tempos_diferentes_com_filtro_por_servico()
    {
        var (osA, servicoA) = await OsAprovada();
        var (osB, servicoB) = await OsAprovada();
        await Executar(osA, TimeSpan.FromHours(1), entregar: false);
        await Executar(osB, TimeSpan.FromHours(3), entregar: true);
        var gerente = Como("gerente");

        var geral = await gerente.GetFromJsonAsync<JsonElement>($"{Rota}/tempo-medio-de-execucao");
        Assert.True(geral.GetProperty("quantidadeDeOs").GetInt32() >= 2);

        var deA = await gerente.GetFromJsonAsync<JsonElement>($"{Rota}/tempo-medio-de-execucao?servicoId={servicoA}");
        Assert.Equal(1, deA.GetProperty("quantidadeDeOs").GetInt32());
        Assert.Equal(3600, deA.GetProperty("tempoMedioEmSegundos").GetDouble());

        var deB = await gerente.GetFromJsonAsync<JsonElement>($"{Rota}/tempo-medio-de-execucao?servicoId={servicoB}");
        Assert.Equal(10800, deB.GetProperty("tempoMedioEmSegundos").GetDouble());
    }

    [Fact]
    public async Task Sem_os_elegivel_retorna_vazio()
    {
        var resposta = await Como("gerente").GetAsync($"{Rota}/tempo-medio-de-execucao?servicoId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, corpo.GetProperty("quantidadeDeOs").GetInt32());
        Assert.Equal(JsonValueKind.Null, corpo.GetProperty("tempoMedioEmSegundos").ValueKind);
    }

    [Fact]
    public async Task Apenas_gerente_acessa()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Como("atendente").GetAsync($"{Rota}/tempo-medio-de-execucao")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Como("mecanico").GetAsync($"{Rota}/tempo-medio-de-execucao")).StatusCode);
    }
}
