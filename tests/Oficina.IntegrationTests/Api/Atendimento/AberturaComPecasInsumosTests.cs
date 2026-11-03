using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Catalogo;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class AberturaComPecasInsumosTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";

    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Como(string papel) => _factory.CriarClienteComPapel(papel);

    private sealed record Abertura(Guid ClienteId, Guid VeiculoId, Guid ServicoId);

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

    private async Task<Abertura> NovaAbertura()
    {
        var servico = (await (await Como("gerente").PostAsJsonAsync("/api/administrativo/servicos",
            new { nome = $"Servico {Guid.NewGuid():N}", descricao = (string?)null, preco = 150m })).Content.ReadFromJsonAsync<ServicoDto>())!;
        var atendente = Como("atendente");
        var cliente = (await (await atendente.PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = CpfNovo(), email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null })).Content.ReadFromJsonAsync<ClienteDto>())!;
        var veiculo = (await (await atendente.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id })).Content.ReadFromJsonAsync<VeiculoDto>())!;
        return new Abertura(cliente.Id, veiculo.Id, servico.Id);
    }

    private async Task<Guid> CriarPeca(string tipo, decimal saldo)
    {
        var gerente = Como("gerente");
        var criada = await gerente.PostAsJsonAsync("/api/pecas-insumos",
            new { nome = $"Item {Guid.NewGuid():N}", tipo, unidadeDeMedida = tipo == "Peca" ? "un" : "L", preco = 25m });
        var id = (await criada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await gerente.PostAsJsonAsync($"/api/pecas-insumos/{id}/entradas", new { quantidade = saldo })).EnsureSuccessStatusCode();
        return id;
    }

    private async Task<decimal> Saldo(Guid pecaId) =>
        (await Como("mecanico").GetFromJsonAsync<JsonElement>($"/api/pecas-insumos/{pecaId}/saldo")).GetProperty("saldo").GetDecimal();

    private Task<HttpResponseMessage> Abrir(Abertura abertura, params (Guid PecaInsumoId, decimal Quantidade)[] pecasInsumos) =>
        Como("atendente").PostAsJsonAsync(Rota, new
        {
            clienteId = abertura.ClienteId,
            veiculoId = abertura.VeiculoId,
            servicoIds = new[] { abertura.ServicoId },
            pecasInsumos = pecasInsumos.Select(p => new { pecaInsumoId = p.PecaInsumoId, quantidade = p.Quantidade })
        });

    private async Task<OrdemDeServicoResumoDto> AbrirComSucesso(Abertura abertura, params (Guid PecaInsumoId, decimal Quantidade)[] pecasInsumos)
    {
        var resposta = await Abrir(abertura, pecasInsumos);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;
    }

    private Task<OrdemDeServicoDetalheDto?> Detalhe(Guid osId) =>
        Como("atendente").GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{osId}");

    private static int Sequencia(string numero) => int.Parse(numero[^5..]);

    [Fact]
    public async Task Abertura_com_pecas_e_insumos_da_baixa_e_mostra_os_itens_no_detalhe()
    {
        var abertura = await NovaAbertura();
        var filtro = await CriarPeca("Peca", 10m);
        var oleo = await CriarPeca("Insumo", 5m);

        var os = await AbrirComSucesso(abertura, (filtro, 1m), (oleo, 3.5m));

        Assert.Equal("Recebida", os.Status);
        Assert.Equal((9m, 1.5m), (await Saldo(filtro), await Saldo(oleo)));
        var detalhe = (await Detalhe(os.Id))!;
        Assert.Equal([(filtro, 1m), (oleo, 3.5m)], detalhe.PecasInsumos.Select(p => (p.PecaInsumoId, p.Quantidade)).OrderBy(p => p.Quantidade));
    }

    [Fact]
    public async Task Abertura_recusada_nao_grava_os_nem_baixa_nem_consome_numero()
    {
        var abertura = await NovaAbertura();
        var filtro = await CriarPeca("Peca", 10m);
        var escassa = await CriarPeca("Peca", 1m);
        var inativa = await CriarPeca("Peca", 10m);
        (await Como("gerente").DeleteAsync($"/api/pecas-insumos/{inativa}")).EnsureSuccessStatusCode();
        var anterior = await AbrirComSucesso(await NovaAbertura());

        var recusas = new[]
        {
            (HttpStatusCode.UnprocessableEntity, await Abrir(abertura, (filtro, 1m), (escassa, 2m))),
            (HttpStatusCode.Conflict, await Abrir(abertura, (filtro, 1m), (inativa, 1m))),
            (HttpStatusCode.UnprocessableEntity, await Abrir(abertura, (filtro, 1m), (filtro, 1m))),
            (HttpStatusCode.UnprocessableEntity, await Abrir(abertura, (filtro, 1.5m))),
            (HttpStatusCode.NotFound, await Abrir(abertura, (filtro, 1m), (Guid.NewGuid(), 1m)))
        };

        Assert.All(recusas, r => Assert.Equal(r.Item1, r.Item2.StatusCode));
        Assert.Equal((10m, 1m), (await Saldo(filtro), await Saldo(escassa)));
        var aberta = await AbrirComSucesso(abertura);
        Assert.Equal(Sequencia(anterior.Numero) + 1, Sequencia(aberta.Numero));
    }

    [Fact]
    public async Task Pecas_da_abertura_seguem_o_diagnostico_com_estorno_na_remocao_e_entram_na_versao()
    {
        var filtro = await CriarPeca("Peca", 10m);
        var oleo = await CriarPeca("Insumo", 5m);
        var os = await AbrirComSucesso(await NovaAbertura(), (filtro, 2m), (oleo, 1m));
        var mecanico = Como("mecanico");
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{os.Id}/diagnostico/iniciar", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.DeleteAsync($"{Rota}/{os.Id}/pecas-insumos/{oleo}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await mecanico.PostAsync($"{Rota}/{os.Id}/diagnostico/concluir", null)).StatusCode);

        Assert.Equal((8m, 5m), (await Saldo(filtro), await Saldo(oleo)));
        var versao = Assert.Single((await Detalhe(os.Id))!.VersoesDoOrcamento);
        var item = Assert.Single(versao.Itens, i => i.Tipo == "PecaInsumo");
        Assert.Equal((filtro, 2m), (item.ReferenciaId, item.Quantidade));
    }

    [Fact]
    public async Task Pecas_da_abertura_nao_podem_ser_ajustadas_com_a_os_recebida()
    {
        var filtro = await CriarPeca("Peca", 10m);
        var os = await AbrirComSucesso(await NovaAbertura(), (filtro, 2m));
        var mecanico = Como("mecanico");

        Assert.Equal(HttpStatusCode.Conflict, (await mecanico.DeleteAsync($"{Rota}/{os.Id}/pecas-insumos/{filtro}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await mecanico.PostAsJsonAsync($"{Rota}/{os.Id}/pecas-insumos", new { pecaInsumoId = filtro, quantidade = 1m })).StatusCode);
        Assert.Equal(8m, await Saldo(filtro));
    }

    [Fact]
    public async Task Cancelar_os_recebida_estorna_as_pecas_da_abertura()
    {
        var filtro = await CriarPeca("Peca", 10m);
        var oleo = await CriarPeca("Insumo", 5m);
        var os = await AbrirComSucesso(await NovaAbertura(), (filtro, 3m), (oleo, 2.25m));

        var resposta = await Como("atendente").PostAsJsonAsync($"{Rota}/{os.Id}/cancelar", new { motivo = "Cliente desistiu" });

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal((10m, 5m), (await Saldo(filtro), await Saldo(oleo)));
        Assert.Empty((await Detalhe(os.Id))!.PecasInsumos);
    }
}
