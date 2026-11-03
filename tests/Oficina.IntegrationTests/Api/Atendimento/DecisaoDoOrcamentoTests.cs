using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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
public class DecisaoDoOrcamentoTests : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";
    private const string RotaPublica = "/api/publico/orcamentos";

    private readonly ApiFactory _factory;

    public DecisaoDoOrcamentoTests(PostgresFixture postgres)
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
        var antes = _factory.OrcamentosNotificados().Count;
        Assert.Equal(HttpStatusCode.NoContent, (await Como("mecanico").PostAsync($"{Rota}/{osId}/diagnostico/concluir", null)).StatusCode);
        Assert.Equal(antes + 1, _factory.OrcamentosNotificados().Count);
        return _factory.UltimoTokenNotificado();
    }

    private async Task<decimal> Saldo(Guid pecaId)
    {
        var json = await Como("gerente").GetFromJsonAsync<JsonElement>($"/api/pecas-insumos/{pecaId}");
        return json.GetProperty("saldo").GetDecimal();
    }

    private Task<HttpResponseMessage> Decidir(string token, object corpo) =>
        _factory.CreateClient().PostAsJsonAsync($"{RotaPublica}/{token}/decisao", corpo);

    private async Task<JsonElement> Detalhe(Guid osId) =>
        await Como("atendente").GetFromJsonAsync<JsonElement>($"{Rota}/{osId}");

    [Fact]
    public async Task Ciclo_recusa_novo_diagnostico_nova_versao_aprovacao_e_rejeicao_do_token_antigo()
    {
        var (osId, pecaId, cpf, numero) = await OsEmDiagnosticoComPeca();
        var tokenAntigo = await Concluir(osId);
        var saldoAntes = await Saldo(pecaId);
        var publico = _factory.CreateClient();

        var recusa = await Decidir(tokenAntigo, new { decisao = "Recusar", motivo = "Muito caro" });

        Assert.Equal(HttpStatusCode.NoContent, recusa.StatusCode);
        Assert.Equal(saldoAntes, await Saldo(pecaId));
        var detalhe = await Detalhe(osId);
        Assert.Equal("EmDiagnostico", detalhe.GetProperty("status").GetString());
        Assert.Equal("Muito caro", detalhe.GetProperty("motivoDaUltimaRecusa").GetString());

        var tokenNovo = await Concluir(osId);
        Assert.NotEqual(tokenAntigo, tokenNovo);

        Assert.Equal(HttpStatusCode.Conflict, (await Decidir(tokenAntigo, new { decisao = "Aprovar" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Decidir(tokenNovo, new { decisao = "Aprovar" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Decidir(tokenNovo, new { decisao = "Aprovar" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Decidir(tokenAntigo, new { decisao = "Recusar" })).StatusCode);

        var final = await Detalhe(osId);
        Assert.Equal("Aprovada", final.GetProperty("status").GetString());

        var consulta = await publico.PostAsJsonAsync("/api/publico/ordens-de-servico/consulta", new { numero, documento = cpf });
        var corpo = await consulta.Content.ReadAsStringAsync();
        Assert.Contains("Aprovada", corpo);
        Assert.DoesNotContain(tokenNovo, corpo);
    }

    public static TheoryData<string> CorposComDecisaoInvalidaOuAusente() => new()
    {
        """{ "decisao": "Talvez" }""",
        """{ "decisao": "aprovado" }""",
        """{ "decisao": 0 }""",
        """{ "decisao": null }""",
        """{ "motivo": "Sem decisao" }""",
        """{}""",
        ""
    };

    [Theory]
    [MemberData(nameof(CorposComDecisaoInvalidaOuAusente))]
    public async Task Decisao_invalida_ou_ausente_e_400_e_nao_altera_a_os(string corpo)
    {
        var (osId, _, _, _) = await OsEmDiagnosticoComPeca();
        var token = await Concluir(osId);

        var resposta = await _factory.CreateClient().PostAsync($"{RotaPublica}/{token}/decisao",
            new StringContent(corpo, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("AguardandoAprovacao", (await Detalhe(osId)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Recusar_sem_motivo_devolve_a_os_ao_diagnostico_sem_motivo_registrado()
    {
        var (osId, _, _, _) = await OsEmDiagnosticoComPeca();
        var token = await Concluir(osId);

        var resposta = await Decidir(token, new { decisao = "Recusar" });

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var detalhe = await Detalhe(osId);
        Assert.Equal("EmDiagnostico", detalhe.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, detalhe.GetProperty("motivoDaUltimaRecusa").ValueKind);
    }

    [Fact]
    public async Task Aprovar_ignora_o_motivo_informado()
    {
        var (osId, _, _, _) = await OsEmDiagnosticoComPeca();
        var token = await Concluir(osId);

        var resposta = await Decidir(token, new { decisao = "Aprovar", motivo = new string('x', 501) });

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var detalhe = await Detalhe(osId);
        Assert.Equal("Aprovada", detalhe.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, detalhe.GetProperty("motivoDaUltimaRecusa").ValueKind);
    }

    [Fact]
    public async Task Recusar_com_motivo_acima_de_500_caracteres_e_422_e_nao_altera_a_os()
    {
        var (osId, _, _, _) = await OsEmDiagnosticoComPeca();
        var token = await Concluir(osId);

        var resposta = await Decidir(token, new { decisao = "Recusar", motivo = new string('x', 501) });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal("AguardandoAprovacao", (await Detalhe(osId)).GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("Aprovar")]
    [InlineData("Recusar")]
    public async Task Token_de_os_cancelada_em_aguardando_aprovacao_e_409(string decisao)
    {
        var (osId, _, _, _) = await OsEmDiagnosticoComPeca();
        var token = await Concluir(osId);
        var cancelamento = await Como("atendente").PostAsJsonAsync($"{Rota}/{osId}/cancelar", new { motivo = "Cliente desistiu" });
        Assert.Equal(HttpStatusCode.NoContent, cancelamento.StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await Decidir(token, new { decisao, motivo = "x" })).StatusCode);
    }

    [Theory]
    [InlineData("Aprovar")]
    [InlineData("Recusar")]
    public async Task Token_inexistente_e_404(string decisao)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Decidir("naoexiste", new { decisao })).StatusCode);
    }

    [Fact]
    public async Task Openapi_documenta_o_endpoint_de_decisao_e_nao_as_rotas_antigas()
    {
        var documento = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var caminhos = documento.GetProperty("paths");

        Assert.False(caminhos.TryGetProperty($"{RotaPublica}/{{token}}/aprovar", out _));
        Assert.False(caminhos.TryGetProperty($"{RotaPublica}/{{token}}/recusar", out _));
        var post = caminhos.GetProperty($"{RotaPublica}/{{token}}/decisao").GetProperty("post");
        var respostas = post.GetProperty("responses").EnumerateObject().Select(r => r.Name).ToList();
        Assert.Equal(["204", "400", "404", "409", "422"], respostas.Order());

        var esquemas = documento.GetProperty("components").GetProperty("schemas");
        var requisicao = esquemas.GetProperty("DecisaoDoOrcamentoRequest");
        Assert.Equal(["decisao"], requisicao.GetProperty("required").EnumerateArray().Select(r => r.GetString()));
        var decisao = esquemas.GetProperty("DecisaoDoOrcamento");
        Assert.Equal(["Aprovar", "Recusar"], decisao.GetProperty("enum").EnumerateArray().Select(v => v.GetString()));
    }

    [Theory]
    [InlineData("aprovar")]
    [InlineData("recusar")]
    public async Task Rotas_separadas_de_aprovar_e_recusar_nao_existem_mais(string rotaAntiga)
    {
        var (osId, _, _, _) = await OsEmDiagnosticoComPeca();
        var token = await Concluir(osId);

        var resposta = await _factory.CreateClient().PostAsJsonAsync($"{RotaPublica}/{token}/{rotaAntiga}", new { motivo = "x" });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("AguardandoAprovacao", (await Detalhe(osId)).GetProperty("status").GetString());
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
