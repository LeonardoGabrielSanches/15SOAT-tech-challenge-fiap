using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Application.Common;
using Oficina.Application.CatalogoEstoque.UseCases;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class DiagnosticoTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";

    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Atendente() => _factory.CriarClienteComPapel("atendente");

    private HttpClient Mecanico() => _factory.CriarClienteComPapel("mecanico");

    private HttpClient Gerente() => _factory.CriarClienteComPapel("gerente");

    private async Task<Guid> CriarPeca(string tipo, decimal saldo)
    {
        var resposta = await Gerente().PostAsJsonAsync("/api/pecas-insumos",
            new { nome = $"Item {Guid.NewGuid():N}", tipo, unidadeDeMedida = tipo == "Peca" ? "un" : "L", preco = 25m });
        resposta.EnsureSuccessStatusCode();
        var id = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await Gerente().PostAsJsonAsync($"/api/pecas-insumos/{id}/entradas", new { quantidade = saldo })).EnsureSuccessStatusCode();
        return id;
    }

    private async Task<decimal> Saldo(Guid pecaId) =>
        (await Mecanico().GetFromJsonAsync<JsonElement>($"/api/pecas-insumos/{pecaId}/saldo")).GetProperty("saldo").GetDecimal();

    private async Task<Guid> CriarServico(string nome)
    {
        var resposta = await Gerente().PostAsJsonAsync("/api/administrativo/servicos", new { nome, descricao = (string?)null, preco = 150m });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ServicoDto>())!.Id;
    }

    private async Task<Guid> AbrirOs(Guid servicoId)
    {
        var http = Atendente();
        var cliente = await http.PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = CpfNovo(), email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null });
        cliente.EnsureSuccessStatusCode();
        var clienteId = (await cliente.Content.ReadFromJsonAsync<ClienteDto>())!.Id;
        var veiculo = await http.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = clienteId });
        veiculo.EnsureSuccessStatusCode();
        var veiculoId = (await veiculo.Content.ReadFromJsonAsync<VeiculoDto>())!.Id;
        var os = await http.PostAsJsonAsync(Rota, new { clienteId, veiculoId, servicoIds = new[] { servicoId } });
        Assert.Equal(HttpStatusCode.Created, os.StatusCode);
        return (await os.Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!.Id;
    }

    private async Task<Guid> OsEmDiagnostico()
    {
        var osId = await AbrirOs(await CriarServico($"Servico {Guid.NewGuid():N}"));
        var inicio = await Mecanico().PostAsync($"{Rota}/{osId}/diagnostico/iniciar", null);
        Assert.Equal(HttpStatusCode.NoContent, inicio.StatusCode);
        return osId;
    }

    private Task<HttpResponseMessage> Incluir(Guid osId, Guid pecaId, decimal quantidade) =>
        Mecanico().PostAsJsonAsync($"{Rota}/{osId}/pecas-insumos", new { pecaInsumoId = pecaId, quantidade });

    private static string PlacaNova() =>
        new string(Enumerable.Range(0, 3).Select(_ => (char)('A' + Random.Shared.Next(26))).ToArray()) + Random.Shared.Next(1000, 10000);

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

    [Fact]
    public async Task Inicia_diagnostico_e_registra_no_historico_com_o_autor()
    {
        var osId = await OsEmDiagnostico();

        var detalhe = (await Mecanico().GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{osId}"))!;

        Assert.Equal("EmDiagnostico", detalhe.Status);
        var ultima = detalhe.Historico[^1];
        Assert.Equal(("Recebida", "EmDiagnostico", "usuario-de-teste"), (ultima.Origem, ultima.Destino, ultima.Autor));
        Assert.Null(detalhe.MotivoDaUltimaRecusa);
    }

    [Fact]
    public async Task Atendente_nao_inicia_diagnostico_nem_mexe_em_pecas()
    {
        var osId = await AbrirOs(await CriarServico("Servico"));

        Assert.Equal(HttpStatusCode.Forbidden, (await Atendente().PostAsync($"{Rota}/{osId}/diagnostico/iniciar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Atendente().PostAsJsonAsync($"{Rota}/{osId}/pecas-insumos", new { pecaInsumoId = Guid.NewGuid(), quantidade = 1 })).StatusCode);
    }

    [Fact]
    public async Task Saldo_acompanha_inclusao_soma_e_remocao_parcial_e_total()
    {
        var osId = await OsEmDiagnostico();
        var filtro = await CriarPeca("Peca", 10m);
        var oleo = await CriarPeca("Insumo", 20m);

        Assert.Equal(HttpStatusCode.NoContent, (await Incluir(osId, filtro, 3m)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Incluir(osId, filtro, 2m)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Incluir(osId, oleo, 4.5m)).StatusCode);
        Assert.Equal(5m, await Saldo(filtro));
        Assert.Equal(15.5m, await Saldo(oleo));
        var detalhe = (await Mecanico().GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{osId}"))!;
        Assert.Equal(5m, detalhe.PecasInsumos.Single(i => i.PecaInsumoId == filtro).Quantidade);

        Assert.Equal(HttpStatusCode.NoContent, (await Mecanico().DeleteAsync($"{Rota}/{osId}/pecas-insumos/{filtro}?quantidade=2")).StatusCode);
        Assert.Equal(7m, await Saldo(filtro));
        Assert.Equal(HttpStatusCode.NoContent, (await Mecanico().DeleteAsync($"{Rota}/{osId}/pecas-insumos/{filtro}")).StatusCode);
        Assert.Equal(10m, await Saldo(filtro));
        Assert.Equal(HttpStatusCode.NoContent, (await Mecanico().DeleteAsync($"{Rota}/{osId}/pecas-insumos/{oleo}")).StatusCode);
        Assert.Equal(20m, await Saldo(oleo));

        detalhe = (await Mecanico().GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{osId}"))!;
        Assert.Empty(detalhe.PecasInsumos);
    }

    [Fact]
    public async Task Saldo_insuficiente_e_422_e_nao_altera_o_saldo()
    {
        var osId = await OsEmDiagnostico();
        var peca = await CriarPeca("Peca", 2m);

        var resposta = await Incluir(osId, peca, 3m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(2m, await Saldo(peca));
    }

    [Fact]
    public async Task Peca_com_fracao_e_422_e_peca_inativa_e_409()
    {
        var osId = await OsEmDiagnostico();
        var peca = await CriarPeca("Peca", 5m);
        var inativa = await CriarPeca("Peca", 5m);
        await Gerente().DeleteAsync($"/api/pecas-insumos/{inativa}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Incluir(osId, peca, 1.5m)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Incluir(osId, inativa, 1m)).StatusCode);
    }

    [Fact]
    public async Task Incluir_e_remover_fora_de_em_diagnostico_e_409()
    {
        var osId = await AbrirOs(await CriarServico("Servico"));
        var peca = await CriarPeca("Peca", 5m);

        Assert.Equal(HttpStatusCode.Conflict, (await Incluir(osId, peca, 1m)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Mecanico().DeleteAsync($"{Rota}/{osId}/pecas-insumos/{peca}")).StatusCode);
        Assert.Equal(5m, await Saldo(peca));
    }

    [Fact]
    public async Task Mecanico_inclui_e_remove_servico_em_diagnostico_e_repetido_e_409()
    {
        var osId = await OsEmDiagnostico();
        var novo = await CriarServico("Alinhamento");

        Assert.Equal(HttpStatusCode.NoContent, (await Mecanico().PostAsJsonAsync($"{Rota}/{osId}/servicos", new { servicoId = novo })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Mecanico().PostAsJsonAsync($"{Rota}/{osId}/servicos", new { servicoId = novo })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Mecanico().DeleteAsync($"{Rota}/{osId}/servicos/{novo}")).StatusCode);
    }

    [Fact]
    public async Task Conflito_de_concorrencia_e_409_sem_gravacao_parcial()
    {
        var osId = await OsEmDiagnostico();
        var peca = await CriarPeca("Peca", 10m);
        Assert.Equal(HttpStatusCode.NoContent, (await Incluir(osId, peca, 1m)).StatusCode);

        using var escopoA = _factory.Services.CreateScope();
        using var escopoB = _factory.Services.CreateScope();
        var casoA = escopoA.ServiceProvider.GetRequiredService<IncluirPecaInsumoNaOs>();
        var osB = await escopoB.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>().ObterPorId(osId, default);
        var pecaB = await escopoB.ServiceProvider.GetRequiredService<IPecaInsumoRepository>().Obter(peca, default);
        osB!.IncluirPecaInsumo(pecaB!, Quantidade.Criar(4m, TipoPecaInsumo.Peca), DateTimeOffset.UtcNow);

        await casoA.Executar(osId, peca, 3m, default);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            escopoB.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>().Confirmar(default));
        Assert.Equal(6m, await Saldo(peca));
        var detalhe = (await Mecanico().GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{osId}"))!;
        Assert.Equal(4m, detalhe.PecasInsumos.Single().Quantidade);
    }
}
