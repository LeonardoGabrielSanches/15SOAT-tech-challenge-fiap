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
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class AprovacaoDoOrcamentoTests : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";
    private const string RotaPublica = "/api/publico/orcamentos";

    private readonly ApiFactory _origem;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly List<string> _tokens = [];

    public AprovacaoDoOrcamentoTests(PostgresFixture postgres)
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

    private async Task<(Guid OsId, Guid PecaId, string Cpf, string Numero)> OsEmDiagnosticoComPeca()
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
        Assert.Equal(HttpStatusCode.NoContent, (await Como("mecanico").PostAsync($"{Rota}/{os.Id}/diagnostico/iniciar", null)).StatusCode);

        var gerente = Como("gerente");
        var criada = await gerente.PostAsJsonAsync("/api/pecas-insumos",
            new { nome = $"Item {Guid.NewGuid():N}", tipo = "Peca", unidadeDeMedida = "un", preco = 25m });
        var pecaId = (await criada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await gerente.PostAsJsonAsync($"/api/pecas-insumos/{pecaId}/entradas", new { quantidade = 10m })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent,
            (await Como("mecanico").PostAsJsonAsync($"{Rota}/{os.Id}/pecas-insumos", new { pecaInsumoId = pecaId, quantidade = 2m })).StatusCode);
        return (os.Id, pecaId, cpf, os.Numero);
    }

    private async Task<string> Concluir(Guid osId)
    {
        var antes = _tokens.Count;
        Assert.Equal(HttpStatusCode.NoContent, (await Como("mecanico").PostAsync($"{Rota}/{osId}/diagnostico/concluir", null)).StatusCode);
        Assert.Equal(antes + 1, _tokens.Count);
        return _tokens[^1];
    }

    private async Task<decimal> Saldo(Guid pecaId)
    {
        var json = await Como("gerente").GetFromJsonAsync<JsonElement>($"/api/pecas-insumos/{pecaId}");
        return json.GetProperty("saldo").GetDecimal();
    }

    [Fact]
    public async Task Ciclo_recusa_novo_diagnostico_nova_versao_aprovacao_e_rejeicao_do_token_antigo()
    {
        var (osId, pecaId, cpf, numero) = await OsEmDiagnosticoComPeca();
        var tokenAntigo = await Concluir(osId);
        var saldoAntes = await Saldo(pecaId);
        var publico = _factory.CreateClient();

        var recusa = await publico.PostAsJsonAsync($"{RotaPublica}/{tokenAntigo}/recusar", new { motivo = "Muito caro" });

        Assert.Equal(HttpStatusCode.NoContent, recusa.StatusCode);
        Assert.Equal(saldoAntes, await Saldo(pecaId));
        var detalhe = (await Como("atendente").GetFromJsonAsync<JsonElement>($"{Rota}/{osId}"));
        Assert.Equal("EmDiagnostico", detalhe.GetProperty("status").GetString());
        Assert.Equal("Muito caro", detalhe.GetProperty("motivoDaUltimaRecusa").GetString());

        var tokenNovo = await Concluir(osId);
        Assert.NotEqual(tokenAntigo, tokenNovo);

        Assert.Equal(HttpStatusCode.Conflict, (await publico.PostAsync($"{RotaPublica}/{tokenAntigo}/aprovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await publico.PostAsync($"{RotaPublica}/{tokenNovo}/aprovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await publico.PostAsync($"{RotaPublica}/{tokenNovo}/aprovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await publico.PostAsync($"{RotaPublica}/{tokenAntigo}/recusar", null)).StatusCode);

        var final = (await Como("atendente").GetFromJsonAsync<JsonElement>($"{Rota}/{osId}"));
        Assert.Equal("Aprovada", final.GetProperty("status").GetString());

        var consulta = await publico.PostAsJsonAsync("/api/publico/ordens-de-servico/consulta", new { numero, documento = cpf });
        var corpo = await consulta.Content.ReadAsStringAsync();
        Assert.Contains("Aprovada", corpo);
        Assert.DoesNotContain(tokenNovo, corpo);
    }

    [Fact]
    public async Task Token_de_os_cancelada_em_aguardando_aprovacao_e_409_na_aprovacao_e_na_recusa()
    {
        var (osId, _, _, _) = await OsEmDiagnosticoComPeca();
        var token = await Concluir(osId);
        var cancelamento = await Como("atendente").PostAsJsonAsync($"{Rota}/{osId}/cancelar", new { motivo = "Cliente desistiu" });
        Assert.Equal(HttpStatusCode.NoContent, cancelamento.StatusCode);
        var publico = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Conflict, (await publico.PostAsync($"{RotaPublica}/{token}/aprovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await publico.PostAsJsonAsync($"{RotaPublica}/{token}/recusar", new { motivo = "x" })).StatusCode);
    }

    [Fact]
    public async Task Token_inexistente_e_404()
    {
        var publico = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await publico.PostAsync($"{RotaPublica}/naoexiste/aprovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await publico.PostAsync($"{RotaPublica}/naoexiste/recusar", null)).StatusCode);
    }

    [Fact]
    public async Task Aprovacao_e_recusa_simultaneas_com_o_mesmo_token_nao_vencem_ambas()
    {
        var (osId, _, _, _) = await OsEmDiagnosticoComPeca();
        var token = await Concluir(osId);

        using var escopoA = _factory.Services.CreateScope();
        using var escopoB = _factory.Services.CreateScope();
        var osA = await escopoA.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>().ObterPorId(osId, default);
        var osB = await escopoB.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>().ObterPorId(osId, default);
        osA!.AprovarOrcamento(token, "Cliente", DateTimeOffset.UtcNow);
        osB!.RecusarOrcamento(token, "Muito caro", "Cliente", DateTimeOffset.UtcNow);

        await escopoA.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>().Confirmar(default);
        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            escopoB.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>().Confirmar(default));

        var detalhe = (await Como("atendente").GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{osId}"))!;
        Assert.Equal("Aprovada", detalhe.Status);
        Assert.Equal("Aprovada", Assert.Single(detalhe.VersoesDoOrcamento).Situacao);
    }
}
