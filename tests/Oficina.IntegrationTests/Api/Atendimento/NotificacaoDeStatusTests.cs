using System.Net;
using System.Net.Http.Json;
using Moq;
using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Catalogo;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class NotificacaoDeStatusTests : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";
    private const string RotaPublica = "/api/publico/orcamentos";

    private readonly ApiFactory _factory;

    public NotificacaoDeStatusTests(PostgresFixture postgres)
    {
        _factory = new ApiFactory(postgres);
    }

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

    private async Task<ClienteDto> CadastrarCliente() =>
        (await (await Como("atendente").PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = CpfNovo(), email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null }))
            .Content.ReadFromJsonAsync<ClienteDto>())!;

    private async Task<(OrdemDeServicoResumoDto Os, ClienteDto Cliente, VeiculoDto Veiculo)> AbrirOs()
    {
        var servico = (await (await Como("gerente").PostAsJsonAsync("/api/administrativo/servicos",
            new { nome = $"Servico {Guid.NewGuid():N}", descricao = (string?)null, preco = 150m })).Content.ReadFromJsonAsync<ServicoDto>())!;
        var cliente = await CadastrarCliente();
        var atendente = Como("atendente");
        var veiculo = (await (await atendente.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id })).Content.ReadFromJsonAsync<VeiculoDto>())!;
        var resposta = await atendente.PostAsJsonAsync(Rota,
            new { clienteId = cliente.Id, veiculoId = veiculo.Id, servicoIds = new[] { servico.Id } });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return ((await resposta.Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!, cliente, veiculo);
    }

    private async Task Executar(string papel, string rota) =>
        Assert.Equal(HttpStatusCode.NoContent, (await Como(papel).PostAsync(rota, null)).StatusCode);

    private async Task AprovarOrcamento(string token) =>
        Assert.Equal(HttpStatusCode.NoContent,
            (await _factory.CreateClient().PostAsJsonAsync($"{RotaPublica}/{token}/decisao", new { decisao = "Aprovar" })).StatusCode);

    private void VerificarNotificacao(string email, string numero, StatusOs? origem, StatusOs destino) =>
        _factory.Notificador.Verify(n => n.NotificarMudancaDeStatus(
            email, numero, origem, destino, It.IsAny<DateTimeOffset>(), It.IsAny<OrcamentoDaNotificacao?>(), It.IsAny<CancellationToken>()), Times.Once);

    [Fact]
    public async Task Abertura_notifica_o_cliente_da_os_com_origem_vazia()
    {
        var (os, cliente, _) = await AbrirOs();

        _factory.Notificador.Verify(n => n.NotificarMudancaDeStatus(
            cliente.Email, os.Numero, null, StatusOs.Recebida, It.IsAny<DateTimeOffset>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cada_mudanca_de_status_do_fluxo_notifica_o_cliente_com_o_destino()
    {
        var (os, cliente, _) = await AbrirOs();

        await Executar("mecanico", $"{Rota}/{os.Id}/diagnostico/iniciar");
        await Executar("mecanico", $"{Rota}/{os.Id}/diagnostico/concluir");
        await AprovarOrcamento(_factory.UltimoTokenNotificado());
        await Executar("mecanico", $"{Rota}/{os.Id}/execucao/iniciar");
        await Executar("mecanico", $"{Rota}/{os.Id}/execucao/finalizar");
        await Executar("atendente", $"{Rota}/{os.Id}/entrega");

        VerificarNotificacao(cliente.Email, os.Numero, null, StatusOs.Recebida);
        VerificarNotificacao(cliente.Email, os.Numero, StatusOs.Recebida, StatusOs.EmDiagnostico);
        VerificarNotificacao(cliente.Email, os.Numero, StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao);
        VerificarNotificacao(cliente.Email, os.Numero, StatusOs.AguardandoAprovacao, StatusOs.Aprovada);
        VerificarNotificacao(cliente.Email, os.Numero, StatusOs.Aprovada, StatusOs.EmExecucao);
        VerificarNotificacao(cliente.Email, os.Numero, StatusOs.EmExecucao, StatusOs.Finalizada);
        VerificarNotificacao(cliente.Email, os.Numero, StatusOs.Finalizada, StatusOs.Entregue);
        Assert.Equal(7, _factory.Notificador.Invocations.Count);
    }

    [Fact]
    public async Task Cancelamento_notifica_o_cliente()
    {
        var (os, cliente, _) = await AbrirOs();

        Assert.Equal(HttpStatusCode.NoContent,
            (await Como("atendente").PostAsJsonAsync($"{Rota}/{os.Id}/cancelar", new { motivo = "Desistiu" })).StatusCode);

        VerificarNotificacao(cliente.Email, os.Numero, StatusOs.Recebida, StatusOs.Cancelada);
    }

    [Fact]
    public async Task Aguardando_aprovacao_leva_o_orcamento_com_itens_total_e_token_e_os_demais_destinos_nao()
    {
        var (os, cliente, _) = await AbrirOs();
        await Executar("mecanico", $"{Rota}/{os.Id}/diagnostico/iniciar");

        await Executar("mecanico", $"{Rota}/{os.Id}/diagnostico/concluir");

        var orcamento = Assert.Single(_factory.OrcamentosNotificados());
        Assert.Equal(150m, orcamento.Total);
        var item = Assert.Single(orcamento.Itens);
        Assert.Equal((1m, 150m, 150m), (item.Quantidade, item.PrecoUnitario, item.Subtotal));
        _factory.Notificador.Verify(n => n.NotificarMudancaDeStatus(
            cliente.Email, os.Numero, StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao, It.IsAny<DateTimeOffset>(), orcamento, It.IsAny<CancellationToken>()), Times.Once);
        _factory.Notificador.Verify(n => n.NotificarMudancaDeStatus(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StatusOs?>(), It.IsNotIn(StatusOs.AguardandoAprovacao), It.IsAny<DateTimeOffset>(),
            It.IsNotNull<OrcamentoDaNotificacao>(), It.IsAny<CancellationToken>()), Times.Never);
        await AprovarOrcamento(orcamento.Token);
    }

    [Fact]
    public async Task Notifica_o_cliente_da_os_mesmo_apos_o_veiculo_mudar_de_proprietario()
    {
        var (os, cliente, veiculo) = await AbrirOs();
        var novoProprietario = await CadastrarCliente();
        (await Como("atendente").PostAsJsonAsync($"/api/administrativo/veiculos/{veiculo.Id}/transferencia",
            new { novoProprietarioId = novoProprietario.Id })).EnsureSuccessStatusCode();

        await Executar("mecanico", $"{Rota}/{os.Id}/diagnostico/iniciar");

        VerificarNotificacao(cliente.Email, os.Numero, StatusOs.Recebida, StatusOs.EmDiagnostico);
    }

    [Fact]
    public async Task Falha_do_notificador_nao_altera_a_resposta_nem_desfaz_a_mudanca_gravada()
    {
        var (os, _, _) = await AbrirOs();
        _factory.Notificador
            .Setup(n => n.NotificarMudancaDeStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StatusOs?>(), It.IsAny<StatusOs>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<OrcamentoDaNotificacao?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP fora do ar"));

        var resposta = await Como("mecanico").PostAsync($"{Rota}/{os.Id}/diagnostico/iniciar", null);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var detalhe = (await Como("atendente").GetFromJsonAsync<OrdemDeServicoDetalheDto>($"{Rota}/{os.Id}"))!;
        Assert.Equal("EmDiagnostico", detalhe.Status);
    }
}
