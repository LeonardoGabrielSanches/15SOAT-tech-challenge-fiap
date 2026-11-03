using Microsoft.Extensions.Logging;
using Moq;
using Oficina.Application.Atendimento.Notificacao;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.UnitTests.Domain.Atendimento;

namespace Oficina.UnitTests.Application.Atendimento.Notificacao;

public class NotificarClienteDaMudancaDeStatusTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<INotificador> _notificador = new();
    private readonly Mock<ILogger<NotificarClienteDaMudancaDeStatus>> _logger = new();
    private readonly Cliente _cliente;
    private readonly OrdemDeServico _os;

    public NotificarClienteDaMudancaDeStatusTests()
    {
        _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
        _clientes.Setup(r => r.ObterPorId(_cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_cliente);
        var veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, RelogioFixo.Em(Agora)), _cliente);
        _os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 1), _cliente, veiculo, [Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m))], "atendente-1", Agora);
        _ordens.Setup(r => r.ObterPorId(_os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_os);
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    private NotificarClienteDaMudancaDeStatus Manipulador() =>
        new(_clientes.Object, _ordens.Object, _notificador.Object, _logger.Object);

    private StatusDaOsAlterado Evento(StatusOs? origem, StatusOs destino) =>
        new(_os.Id, _os.Numero.Valor, _cliente.Id, origem, destino, Agora);

    [Theory]
    [InlineData(null, StatusOs.Recebida)]
    [InlineData(StatusOs.Recebida, StatusOs.EmDiagnostico)]
    [InlineData(StatusOs.AguardandoAprovacao, StatusOs.Aprovada)]
    [InlineData(StatusOs.Finalizada, StatusOs.Entregue)]
    [InlineData(StatusOs.Recebida, StatusOs.Cancelada)]
    public async Task Notifica_o_email_do_cliente_da_os_com_origem_e_destino_sem_orcamento(StatusOs? origem, StatusOs destino)
    {
        await Manipulador().Manipular(Evento(origem, destino), CancellationToken.None);

        _notificador.Verify(n => n.NotificarMudancaDeStatus(
            "maria@oficina.com", "OS-2026-00001", origem, destino, Agora, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Notifica_o_email_atual_mesmo_com_o_cliente_inativo()
    {
        _cliente.AtualizarDados("Maria Silva", Email.Criar("maria.nova@oficina.com"), null);
        _cliente.Inativar();

        await Manipulador().Manipular(Evento(StatusOs.EmExecucao, StatusOs.Finalizada), CancellationToken.None);

        _notificador.Verify(n => n.NotificarMudancaDeStatus(
            "maria.nova@oficina.com", "OS-2026-00001", StatusOs.EmExecucao, StatusOs.Finalizada, Agora, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aguardando_aprovacao_leva_a_versao_vigente_com_itens_total_e_token()
    {
        var filtro = PecaInsumo.Criar("Filtro", TipoPecaInsumo.Peca, "un", Dinheiro.De(25m));
        filtro.RegistrarEntrada(Quantidade.Criar(10m, TipoPecaInsumo.Peca));
        var servico = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));
        var os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 2), _cliente,
            Veiculo.Cadastrar(Placa.Criar("XYZ1A23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, RelogioFixo.Em(Agora)), _cliente),
            [servico], "atendente-1", Agora);
        os.IniciarDiagnostico("mecanico-1", Agora);
        os.IncluirPecaInsumo(filtro, Quantidade.Criar(3m, TipoPecaInsumo.Peca), Agora);
        os.ConcluirDiagnostico([servico], [filtro], "mecanico-1", Agora);
        _ordens.Setup(r => r.ObterPorId(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        OrcamentoDaNotificacao? enviado = null;
        _notificador
            .Setup(n => n.NotificarMudancaDeStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StatusOs?>(), It.IsAny<StatusOs>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<OrcamentoDaNotificacao?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, StatusOs?, StatusOs, DateTimeOffset, OrcamentoDaNotificacao?, CancellationToken>(
                (_, _, _, _, _, orcamento, _) => enviado = orcamento)
            .Returns(Task.CompletedTask);

        await Manipulador().Manipular(
            new StatusDaOsAlterado(os.Id, "OS-2026-00002", _cliente.Id, StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao, Agora),
            CancellationToken.None);

        Assert.NotNull(enviado);
        Assert.Equal((175m, os.VersaoVigente!.Token), (enviado.Total, enviado.Token));
        Assert.Equal(
            [
                new ItemDoOrcamentoDaNotificacao("Troca de oleo", 1m, 100m, 100m),
                new ItemDoOrcamentoDaNotificacao("Filtro", 3m, 25m, 75m)
            ],
            enviado.Itens);
    }

    [Fact]
    public async Task Falha_do_notificador_e_registrada_em_log_sem_propagar()
    {
        _notificador
            .Setup(n => n.NotificarMudancaDeStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StatusOs?>(), It.IsAny<StatusOs>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<OrcamentoDaNotificacao?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP fora do ar"));

        await Manipulador().Manipular(Evento(StatusOs.Aprovada, StatusOs.EmExecucao), CancellationToken.None);

        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.Is<InvalidOperationException>(e => e.Message == "SMTP fora do ar"),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
