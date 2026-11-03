using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Catalogo;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class TempoMedioDeExecucaoTests : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";
    private const string RotaPublica = "/api/publico/orcamentos";

    private readonly PostgresFixture _postgres;
    private readonly ApiFactory _origem;
    private readonly WebApplicationFactory<Program> _factory;
    private DateTimeOffset _agora = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    public TempoMedioDeExecucaoTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        _origem = new ApiFactory(postgres);
        _factory = _origem.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
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

    private async Task<ServicoDto> Servico(string nome) =>
        (await (await Como("gerente").PostAsJsonAsync("/api/administrativo/servicos",
            new { nome = $"{nome} {Guid.NewGuid():N}", descricao = (string?)null, preco = 150m })).Content.ReadFromJsonAsync<ServicoDto>())!;

    private async Task<Guid> OsAprovada(params ServicoDto[] servicos)
    {
        var atendente = Como("atendente");
        var cpf = CpfNovo();
        var cliente = (await (await atendente.PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = cpf, email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null })).Content.ReadFromJsonAsync<ClienteDto>())!;
        var veiculo = (await (await atendente.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id })).Content.ReadFromJsonAsync<VeiculoDto>())!;
        var os = (await (await atendente.PostAsJsonAsync(Rota,
            new { clienteId = cliente.Id, veiculoId = veiculo.Id, servicoIds = servicos.Select(s => s.Id).ToArray() })).Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;

        var mecanico = Como("mecanico");
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{os.Id}/diagnostico/iniciar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{os.Id}/diagnostico/concluir", null)).StatusCode);
        var token = _origem.UltimoTokenNotificado();
        Assert.Equal(HttpStatusCode.NoContent, (await _factory.CreateClient().PostAsJsonAsync($"{RotaPublica}/{token}/decisao", new { decisao = "Aprovar" })).StatusCode);
        return os.Id;
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
    public async Task Tempo_medio_geral_e_por_combinacao_de_servicos()
    {
        var oleo = await Servico("Troca de oleo");
        var pneu = await Servico("Troca de pneu");
        var osOleoEPneu = await OsAprovada(pneu, oleo);
        var osOleo = await OsAprovada(oleo);
        await Executar(osOleoEPneu, TimeSpan.FromMinutes(55), entregar: false);
        await Executar(osOleo, TimeSpan.FromMinutes(15), entregar: true);

        var corpo = await Como("gerente").GetFromJsonAsync<JsonElement>($"{Rota}/tempo-medio-de-execucao");

        Assert.True(corpo.GetProperty("quantidadeDeOs").GetInt32() >= 2);
        Assert.Equal(JsonValueKind.Number, corpo.GetProperty("tempoMedioEmSegundos").ValueKind);
        var combinacoes = corpo.GetProperty("combinacoes").EnumerateArray()
            .Where(c => c.GetProperty("servicos").EnumerateArray().Any(s => s.GetProperty("id").GetGuid() == oleo.Id))
            .ToList();
        Assert.Equal(2, combinacoes.Count);

        var soOleo = Assert.Single(combinacoes, c => c.GetProperty("servicos").GetArrayLength() == 1);
        Assert.Equal(1, soOleo.GetProperty("quantidadeDeOs").GetInt32());
        Assert.Equal(900, soOleo.GetProperty("tempoMedioEmSegundos").GetDouble());
        var servicoDoOleo = soOleo.GetProperty("servicos")[0];
        Assert.Equal(oleo.Nome, servicoDoOleo.GetProperty("nome").GetString());
        Assert.True(servicoDoOleo.GetProperty("ativo").GetBoolean());

        var oleoEPneu = Assert.Single(combinacoes, c => c.GetProperty("servicos").GetArrayLength() == 2);
        Assert.Equal(1, oleoEPneu.GetProperty("quantidadeDeOs").GetInt32());
        Assert.Equal(3300, oleoEPneu.GetProperty("tempoMedioEmSegundos").GetDouble());
        Assert.Equal(
            [oleo.Id, pneu.Id],
            oleoEPneu.GetProperty("servicos").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task Sem_os_elegivel_retorna_vazio()
    {
        await using var semOs = _origem.WithWebHostBuilder(b =>
            b.UseSetting("ConnectionStrings:Oficina", _postgres.ConnectionString($"oficina_sem_os_{Guid.NewGuid():N}")));
        var http = semOs.CreateClient();
        http.DefaultRequestHeaders.Add(ApiFactory.CabecalhoPapel, "gerente");

        var resposta = await http.GetAsync($"{Rota}/tempo-medio-de-execucao");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, corpo.GetProperty("quantidadeDeOs").GetInt32());
        Assert.Equal(JsonValueKind.Null, corpo.GetProperty("tempoMedioEmSegundos").ValueKind);
        Assert.Equal(0, corpo.GetProperty("combinacoes").GetArrayLength());
    }

    [Fact]
    public async Task Apenas_gerente_acessa()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Como("atendente").GetAsync($"{Rota}/tempo-medio-de-execucao")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Como("mecanico").GetAsync($"{Rota}/tempo-medio-de-execucao")).StatusCode);
    }
}
