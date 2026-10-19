using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Application.CatalogoEstoque.UseCases;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public partial class OrdensDeServicoTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";

    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [GeneratedRegex(@"^OS-\d{4}-\d{5}$")]
    private static partial Regex FormatoDoNumero();

    private HttpClient Atendente() => _factory.CriarClienteComPapel("atendente");

    private async Task<ServicoDto> CriarServico(string nome = "Troca de oleo")
    {
        var resposta = await _factory.CriarClienteComPapel("gerente")
            .PostAsJsonAsync("/api/administrativo/servicos", new { nome, descricao = (string?)null, preco = 150m });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ServicoDto>())!;
    }

    private async Task<(ClienteDto Cliente, VeiculoDto Veiculo)> CriarClienteComVeiculo()
    {
        var http = Atendente();
        var clienteResposta = await http.PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = CpfNovo(), email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null });
        clienteResposta.EnsureSuccessStatusCode();
        var cliente = (await clienteResposta.Content.ReadFromJsonAsync<ClienteDto>())!;

        var veiculoResposta = await http.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id });
        veiculoResposta.EnsureSuccessStatusCode();
        return (cliente, (await veiculoResposta.Content.ReadFromJsonAsync<VeiculoDto>())!);
    }

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

    private static object Abertura(Guid clienteId, Guid veiculoId, params Guid[] servicoIds) =>
        new { clienteId, veiculoId, servicoIds };

    private async Task<OrdemDeServicoResumoDto> AbrirOs(Guid clienteId, Guid veiculoId, params Guid[] servicoIds)
    {
        var resposta = await Atendente().PostAsJsonAsync(Rota, Abertura(clienteId, veiculoId, servicoIds));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;
    }

    [Fact]
    public async Task Abre_a_os_recebida_com_numero_no_formato_e_detalha_com_historico()
    {
        var servico = await CriarServico();
        var (cliente, veiculo) = await CriarClienteComVeiculo();

        var resposta = await Atendente().PostAsJsonAsync(Rota, Abertura(cliente.Id, veiculo.Id, servico.Id));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var os = (await resposta.Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;
        Assert.Matches(FormatoDoNumero(), os.Numero);
        Assert.Equal("Recebida", os.Status);
        Assert.EndsWith($"{Rota}/{os.Id}", resposta.Headers.Location!.ToString());

        var detalhe = (await Atendente().GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{os.Id}"))!;
        Assert.Equal(os.Numero, detalhe.Numero);
        Assert.Equal(cliente.Id, detalhe.Cliente.Id);
        Assert.Equal(veiculo.Placa, detalhe.Veiculo.Placa);
        Assert.Equal([servico.Id], detalhe.Servicos.Select(s => s.ServicoId));
        Assert.Empty(detalhe.VersoesDoOrcamento);
        var entrada = Assert.Single(detalhe.Historico);
        Assert.Equal((null, "Recebida", "usuario-de-teste"), (entrada.Origem, entrada.Destino, entrada.Autor));
    }

    [Fact]
    public async Task Numeros_sao_sequenciais_no_ano()
    {
        var servico = await CriarServico();
        var (c1, v1) = await CriarClienteComVeiculo();
        var (c2, v2) = await CriarClienteComVeiculo();

        var primeira = await AbrirOs(c1.Id, v1.Id, servico.Id);
        var segunda = await AbrirOs(c2.Id, v2.Id, servico.Id);

        var sequencia = (string n) => int.Parse(n[^5..]);
        Assert.Equal(sequencia(primeira.Numero) + 1, sequencia(segunda.Numero));
    }

    [Fact]
    public async Task Segunda_os_em_andamento_para_o_mesmo_veiculo_e_409()
    {
        var servico = await CriarServico();
        var (cliente, veiculo) = await CriarClienteComVeiculo();
        await AbrirOs(cliente.Id, veiculo.Id, servico.Id);

        var resposta = await Atendente().PostAsJsonAsync(Rota, Abertura(cliente.Id, veiculo.Id, servico.Id));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Rejeita_cliente_que_nao_e_proprietario_e_os_sem_servico()
    {
        var servico = await CriarServico();
        var (_, veiculo) = await CriarClienteComVeiculo();
        var (outro, _) = await CriarClienteComVeiculo();

        var outroDono = await Atendente().PostAsJsonAsync(Rota, Abertura(outro.Id, veiculo.Id, servico.Id));
        var semServico = await Atendente().PostAsJsonAsync(Rota, Abertura(outro.Id, veiculo.Id));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, outroDono.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semServico.StatusCode);
    }

    [Fact]
    public async Task Rejeita_servico_cliente_ou_veiculo_inativos()
    {
        var ativo = await CriarServico();
        var inativo = await CriarServico("Antigo");
        await _factory.CriarClienteComPapel("gerente").DeleteAsync($"/api/administrativo/servicos/{inativo.Id}");
        var (cliente, veiculo) = await CriarClienteComVeiculo();
        var (clienteInativo, _) = await CriarClienteComVeiculo();
        var (outroCliente, veiculoInativo) = await CriarClienteComVeiculo();
        await Atendente().DeleteAsync($"/api/administrativo/veiculos/{veiculoInativo.Id}");
        await Atendente().DeleteAsync($"/api/administrativo/clientes/{clienteInativo.Id}");

        var comServicoInativo = await Atendente().PostAsJsonAsync(Rota, Abertura(cliente.Id, veiculo.Id, ativo.Id, inativo.Id));
        var comVeiculoInativo = await Atendente().PostAsJsonAsync(Rota, Abertura(outroCliente.Id, veiculoInativo.Id, ativo.Id));

        Assert.Equal(HttpStatusCode.Conflict, comServicoInativo.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, comVeiculoInativo.StatusCode);
    }

    [Fact]
    public async Task Inclui_e_remove_servico_e_rejeita_repetido()
    {
        var primeiro = await CriarServico("Alinhamento");
        var segundo = await CriarServico("Balanceamento");
        var (cliente, veiculo) = await CriarClienteComVeiculo();
        var os = await AbrirOs(cliente.Id, veiculo.Id, primeiro.Id);
        var http = Atendente();

        var inclusao = await http.PostAsJsonAsync($"{Rota}/{os.Id}/servicos", new { servicoId = segundo.Id });
        var repetido = await http.PostAsJsonAsync($"{Rota}/{os.Id}/servicos", new { servicoId = segundo.Id });
        var remocao = await http.DeleteAsync($"{Rota}/{os.Id}/servicos/{primeiro.Id}");
        var ultimo = await http.DeleteAsync($"{Rota}/{os.Id}/servicos/{segundo.Id}");

        Assert.Equal(HttpStatusCode.NoContent, inclusao.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, remocao.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ultimo.StatusCode);
        var detalhe = (await http.GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{os.Id}"))!;
        Assert.Equal([segundo.Id], detalhe.Servicos.Select(s => s.ServicoId));
    }

    [Fact]
    public async Task Lista_ordenada_por_prioridade_com_filtro_de_status()
    {
        var servico = await CriarServico();
        var (cliente, veiculo) = await CriarClienteComVeiculo();
        var os = await AbrirOs(cliente.Id, veiculo.Id, servico.Id);
        var http = Atendente();

        var lista = (await http.GetFromJsonAsync<List<OrdemDeServicoResumoDto>>(Rota))!;
        var recebidas = (await http.GetFromJsonAsync<List<OrdemDeServicoResumoDto>>($"{Rota}?status=Recebida"))!;
        var entregues = (await http.GetFromJsonAsync<List<OrdemDeServicoResumoDto>>($"{Rota}?status=Entregue"))!;
        var invalido = await http.GetAsync($"{Rota}?status=Inexistente");

        Assert.Contains(lista, o => o.Id == os.Id);
        Assert.Contains(recebidas, o => o.Id == os.Id);
        Assert.DoesNotContain(entregues, o => o.Id == os.Id);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalido.StatusCode);
    }

    [Fact]
    public async Task Mecanico_lista_as_os_com_e_sem_filtro_de_status()
    {
        var servico = await CriarServico();
        var (cliente, veiculo) = await CriarClienteComVeiculo();
        var os = await AbrirOs(cliente.Id, veiculo.Id, servico.Id);
        var mecanico = _factory.CriarClienteComPapel("mecanico");

        var lista = await mecanico.GetAsync(Rota);
        var recebidas = await mecanico.GetAsync($"{Rota}?status=Recebida");
        var entregues = await mecanico.GetAsync($"{Rota}?status=Entregue");

        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        Assert.Contains((await lista.Content.ReadFromJsonAsync<List<OrdemDeServicoResumoDto>>())!, o => o.Id == os.Id);
        Assert.Contains((await recebidas.Content.ReadFromJsonAsync<List<OrdemDeServicoResumoDto>>())!, o => o.Id == os.Id);
        Assert.DoesNotContain((await entregues.Content.ReadFromJsonAsync<List<OrdemDeServicoResumoDto>>())!, o => o.Id == os.Id);
    }

    [Fact]
    public async Task Detalhe_de_os_inexistente_e_404_e_sem_papel_adequado_e_403()
    {
        var http = Atendente();

        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"{Rota}/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _factory.CriarClienteComPapel("visitante").GetAsync(Rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(Rota)).StatusCode);
    }

    [Fact]
    public async Task Papel_do_ajuste_de_servicos_depende_do_status_da_os()
    {
        var primeiro = await CriarServico("Alinhamento");
        var segundo = await CriarServico("Balanceamento");
        var terceiro = await CriarServico("Revisao");
        var (cliente, veiculo) = await CriarClienteComVeiculo();
        var os = await AbrirOs(cliente.Id, veiculo.Id, primeiro.Id);
        var atendente = Atendente();
        var mecanico = _factory.CriarClienteComPapel("mecanico");
        var gerente = _factory.CriarClienteComPapel("gerente");

        var mecanicoEmRecebida = await mecanico.PostAsJsonAsync($"{Rota}/{os.Id}/servicos", new { servicoId = segundo.Id });
        var atendenteEmRecebida = await atendente.PostAsJsonAsync($"{Rota}/{os.Id}/servicos", new { servicoId = segundo.Id });
        var mecanicoRemoveEmRecebida = await mecanico.DeleteAsync($"{Rota}/{os.Id}/servicos/{segundo.Id}");
        var gerenteEmRecebida = await gerente.PostAsJsonAsync($"{Rota}/{os.Id}/servicos", new { servicoId = terceiro.Id });
        await mecanico.PostAsync($"{Rota}/{os.Id}/diagnostico/iniciar", null);
        var atendenteEmDiagnostico = await atendente.DeleteAsync($"{Rota}/{os.Id}/servicos/{segundo.Id}");
        var atendenteIncluiEmDiagnostico = await atendente.PostAsJsonAsync($"{Rota}/{os.Id}/servicos", new { servicoId = primeiro.Id });
        var mecanicoEmDiagnostico = await mecanico.DeleteAsync($"{Rota}/{os.Id}/servicos/{segundo.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, mecanicoEmRecebida.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, atendenteEmRecebida.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, mecanicoRemoveEmRecebida.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, gerenteEmRecebida.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, atendenteEmDiagnostico.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, atendenteIncluiEmDiagnostico.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, mecanicoEmDiagnostico.StatusCode);
    }

    [Fact]
    public async Task Listagem_sem_filtro_omite_entregues_e_canceladas_no_banco()
    {
        var servico = await CriarServico();
        var (cliente, veiculo) = await CriarClienteComVeiculo();
        var os = await AbrirOs(cliente.Id, veiculo.Id, servico.Id);
        await Atendente().PostAsJsonAsync($"{Rota}/{os.Id}/cancelar", new { motivo = "Desistencia" });

        var lista = (await Atendente().GetFromJsonAsync<List<OrdemDeServicoResumoDto>>(Rota))!;
        var canceladas = (await Atendente().GetFromJsonAsync<List<OrdemDeServicoResumoDto>>($"{Rota}?status=Cancelada"))!;

        Assert.DoesNotContain(lista, o => o.Id == os.Id);
        Assert.Contains(canceladas, o => o.Id == os.Id);
    }
}
