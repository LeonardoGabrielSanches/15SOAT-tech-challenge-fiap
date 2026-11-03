using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Catalogo;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class ConclusaoDoDiagnosticoTests : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";

    private readonly ApiFactory _factory;

    public ConclusaoDoDiagnosticoTests(PostgresFixture postgres)
    {
        _factory = new ApiFactory(postgres);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

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

    private async Task<(Guid OsId, string Numero, string Cpf, string Email)> OsEmDiagnostico()
    {
        var servicoResposta = await Como("gerente").PostAsJsonAsync("/api/administrativo/servicos",
            new { nome = $"Servico {Guid.NewGuid():N}", descricao = (string?)null, preco = 150m });
        var servico = (await servicoResposta.Content.ReadFromJsonAsync<ServicoDto>())!;

        var atendente = Como("atendente");
        var cpf = CpfNovo();
        var email = $"{Guid.NewGuid():N}@oficina.com";
        var cliente = (await (await atendente.PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = cpf, email, telefone = (string?)null })).Content.ReadFromJsonAsync<ClienteDto>())!;
        var veiculo = (await (await atendente.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id })).Content.ReadFromJsonAsync<VeiculoDto>())!;
        var os = (await (await atendente.PostAsJsonAsync(Rota,
            new { clienteId = cliente.Id, veiculoId = veiculo.Id, servicoIds = new[] { servico.Id } })).Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await Como("mecanico").PostAsync($"{Rota}/{os.Id}/diagnostico/iniciar", null)).StatusCode);
        return (os.Id, os.Numero, cpf, email);
    }

    private async Task IncluirPeca(Guid osId, decimal preco, decimal saldo, decimal quantidade)
    {
        var gerente = Como("gerente");
        var criada = await gerente.PostAsJsonAsync("/api/pecas-insumos",
            new { nome = $"Item {Guid.NewGuid():N}", tipo = "Peca", unidadeDeMedida = "un", preco });
        var id = (await criada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await gerente.PostAsJsonAsync($"/api/pecas-insumos/{id}/entradas", new { quantidade = saldo })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent,
            (await Como("mecanico").PostAsJsonAsync($"{Rota}/{osId}/pecas-insumos", new { pecaInsumoId = id, quantidade })).StatusCode);
    }

    [Fact]
    public async Task Conclusao_gera_versao_move_status_e_notifica_com_o_token_apos_o_commit()
    {
        var (osId, numero, cpf, email) = await OsEmDiagnostico();
        await IncluirPeca(osId, 25m, 10m, 2m);

        var conclusao = await Como("mecanico").PostAsync($"{Rota}/{osId}/diagnostico/concluir", null);

        Assert.Equal(HttpStatusCode.NoContent, conclusao.StatusCode);
        var envio = Assert.Single(_factory.OrcamentosNotificados());
        Assert.Equal(200m, envio.Total);
        Assert.False(string.IsNullOrWhiteSpace(envio.Token));
        var notificacao = Assert.Single(_factory.NotificacoesEnviadas(), n => n.Orcamento is not null);
        Assert.Equal((email, numero, StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao, envio),
            (notificacao.Email, notificacao.NumeroOs, notificacao.Origem, notificacao.Destino, notificacao.Orcamento));
        var detalhe = (await Como("atendente").GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{osId}"))!;
        Assert.Equal("AguardandoAprovacao", detalhe.Status);
        var versao = Assert.Single(detalhe.VersoesDoOrcamento);
        Assert.Equal((1, 200m, "Pendente"), (versao.Numero, versao.Total, versao.Situacao));

        var consultaHttp = await _factory.CreateClient().PostAsJsonAsync("/api/publico/ordens-de-servico/consulta", new { numero, documento = cpf });
        var corpo = await consultaHttp.Content.ReadAsStringAsync();
        Assert.DoesNotContain(envio.Token, corpo);
        var orcamento = (await consultaHttp.Content.ReadFromJsonAsync<ConsultaPublicaDaOsDto>())!.OrcamentoVigente!;
        Assert.Equal((1, 200m, 2), (orcamento.Versao, orcamento.Total, orcamento.Itens.Count));
    }

    [Fact]
    public async Task Conclusao_exige_papel_mecanico_e_status_em_diagnostico()
    {
        var (osId, _, _, _) = await OsEmDiagnostico();

        Assert.Equal(HttpStatusCode.Forbidden, (await Como("atendente").PostAsync($"{Rota}/{osId}/diagnostico/concluir", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Como("mecanico").PostAsync($"{Rota}/{osId}/diagnostico/concluir", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Como("mecanico").PostAsync($"{Rota}/{osId}/diagnostico/concluir", null)).StatusCode);
        Assert.Single(_factory.OrcamentosNotificados());
    }
}
