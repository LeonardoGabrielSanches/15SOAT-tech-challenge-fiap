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
public class CancelamentoDaOsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";

    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Como(string papel) => _factory.CriarClienteComPapel(papel);

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

    private async Task<(Guid OsId, Guid ClienteId, Guid VeiculoId, Guid ServicoId)> OsRecebida()
    {
        var servicoResposta = await Como("gerente").PostAsJsonAsync("/api/administrativo/servicos",
            new { nome = $"Servico {Guid.NewGuid():N}", descricao = (string?)null, preco = 150m });
        var servico = (await servicoResposta.Content.ReadFromJsonAsync<ServicoDto>())!;
        var atendente = Como("atendente");
        var cliente = (await (await atendente.PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = CpfNovo(), email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null })).Content.ReadFromJsonAsync<ClienteDto>())!;
        var veiculo = (await (await atendente.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id })).Content.ReadFromJsonAsync<VeiculoDto>())!;
        var os = (await (await atendente.PostAsJsonAsync(Rota,
            new { clienteId = cliente.Id, veiculoId = veiculo.Id, servicoIds = new[] { servico.Id } })).Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;
        return (os.Id, cliente.Id, veiculo.Id, servico.Id);
    }

    private async Task<Guid> CriarPecaComSaldo(decimal saldo)
    {
        var gerente = Como("gerente");
        var criada = await gerente.PostAsJsonAsync("/api/pecas-insumos",
            new { nome = $"Item {Guid.NewGuid():N}", tipo = "Peca", unidadeDeMedida = "un", preco = 25m });
        var id = (await criada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await gerente.PostAsJsonAsync($"/api/pecas-insumos/{id}/entradas", new { quantidade = saldo })).EnsureSuccessStatusCode();
        return id;
    }

    private async Task<decimal> Saldo(Guid pecaId) =>
        (await Como("mecanico").GetFromJsonAsync<JsonElement>($"/api/pecas-insumos/{pecaId}/saldo")).GetProperty("saldo").GetDecimal();

    private Task<HttpResponseMessage> Cancelar(Guid osId, string? motivo = "Cliente desistiu", string papel = "atendente") =>
        Como(papel).PostAsJsonAsync($"{Rota}/{osId}/cancelar", new { motivo });

    [Fact]
    public async Task Cancelamento_restaura_saldo_invalida_o_token_e_libera_nova_os_para_o_veiculo()
    {
        var (osId, clienteId, veiculoId, servicoId) = await OsRecebida();
        Assert.Equal(HttpStatusCode.NoContent, (await Como("mecanico").PostAsync($"{Rota}/{osId}/diagnostico/iniciar", null)).StatusCode);
        var peca = await CriarPecaComSaldo(10m);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Como("mecanico").PostAsJsonAsync($"{Rota}/{osId}/pecas-insumos", new { pecaInsumoId = peca, quantidade = 4m })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Como("mecanico").PostAsync($"{Rota}/{osId}/diagnostico/concluir", null)).StatusCode);
        Assert.Equal(6m, await Saldo(peca));
        Assert.Equal(HttpStatusCode.Conflict,
            (await Como("atendente").PostAsJsonAsync(Rota, new { clienteId, veiculoId, servicoIds = new[] { servicoId } })).StatusCode);

        var resposta = await Cancelar(osId);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(10m, await Saldo(peca));
        var detalhe = (await Como("atendente").GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{osId}"))!;
        Assert.Equal("Cancelada", detalhe.Status);
        Assert.Empty(detalhe.PecasInsumos);
        Assert.DoesNotContain(detalhe.VersoesDoOrcamento, v => v.Situacao == "Pendente");
        Assert.Equal(("AguardandoAprovacao", "Cancelada"), (detalhe.Historico[^1].Origem, detalhe.Historico[^1].Destino));
        var nova = await Como("atendente").PostAsJsonAsync(Rota, new { clienteId, veiculoId, servicoIds = new[] { servicoId } });
        Assert.Equal(HttpStatusCode.Created, nova.StatusCode);
    }

    [Fact]
    public async Task Cancelamento_exige_atendente_motivo_e_nao_repete()
    {
        var (osId, _, _, _) = await OsRecebida();

        Assert.Equal(HttpStatusCode.Forbidden, (await Cancelar(osId, papel: "mecanico")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Cancelar(osId, motivo: " ")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Cancelar(osId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Cancelar(osId)).StatusCode);
    }
}
