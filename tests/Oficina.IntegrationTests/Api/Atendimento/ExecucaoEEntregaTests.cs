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
public class ExecucaoEEntregaTests : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";
    private const string RotaPublica = "/api/publico/orcamentos";

    private readonly ApiFactory _origem;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<string> _tokens = [];

    public ExecucaoEEntregaTests(PostgresFixture postgres)
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

    private async Task<(Guid OsId, string Cpf, string Numero)> OsAprovada()
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
        return (os.Id, cpf, os.Numero);
    }

    [Fact]
    public async Task Ciclo_completo_da_abertura_ate_entregue_consultado_pela_consulta_publica()
    {
        var (osId, cpf, numero) = await OsAprovada();
        var mecanico = Como("mecanico");
        var atendente = Como("atendente");
        var publico = _factory.CreateClient();

        var fila = await mecanico.GetFromJsonAsync<List<OrdemDeServicoResumoDto>>($"{Rota}/fila-de-execucao");
        Assert.Contains(fila!, o => o.Id == osId);

        Assert.Equal(HttpStatusCode.Conflict, (await mecanico.PostAsync($"{Rota}/{osId}/execucao/finalizar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{osId}/execucao/iniciar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await mecanico.PostAsync($"{Rota}/{osId}/execucao/iniciar", null)).StatusCode);

        var filaDepois = await mecanico.GetFromJsonAsync<List<OrdemDeServicoResumoDto>>($"{Rota}/fila-de-execucao");
        Assert.DoesNotContain(filaDepois!, o => o.Id == osId);

        Assert.Equal(HttpStatusCode.Conflict, (await atendente.PostAsync($"{Rota}/{osId}/entrega", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{osId}/execucao/finalizar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await atendente.PostAsync($"{Rota}/{osId}/entrega", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await atendente.PostAsync($"{Rota}/{osId}/entrega", null)).StatusCode);

        var detalhe = await atendente.GetFromJsonAsync<JsonElement>($"{Rota}/{osId}");
        Assert.Equal("Entregue", detalhe.GetProperty("status").GetString());

        var consulta = await publico.PostAsJsonAsync("/api/publico/ordens-de-servico/consulta", new { numero, documento = cpf });
        var corpo = await consulta.Content.ReadAsStringAsync();
        Assert.Contains("Entregue", corpo);
        Assert.Contains("EmExecucao", corpo);
        Assert.Contains("Finalizada", corpo);
    }

    [Fact]
    public async Task Papeis_sao_respeitados()
    {
        var (osId, _, _) = await OsAprovada();

        Assert.Equal(HttpStatusCode.Forbidden, (await Como("atendente").PostAsync($"{Rota}/{osId}/execucao/iniciar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Como("mecanico").PostAsync($"{Rota}/{osId}/entrega", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Como("atendente").GetAsync($"{Rota}/fila-de-execucao")).StatusCode);
    }
}
