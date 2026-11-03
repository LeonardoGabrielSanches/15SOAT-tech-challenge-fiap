using Microsoft.Extensions.Logging;
using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.Notificacao;

public sealed partial class NotificarClienteDaMudancaDeStatus(
    IClienteRepository clientes,
    IOrdemDeServicoRepository ordens,
    INotificador notificador,
    ILogger<NotificarClienteDaMudancaDeStatus> logger)
    : IManipuladorDeEvento<StatusDaOsAlterado>
{
    public async Task Manipular(StatusDaOsAlterado evento, CancellationToken cancellationToken)
    {
        try
        {
            var cliente = await BuscaDeCliente.ObterOuFalhar(clientes, evento.ClienteId, cancellationToken);
            var orcamento = evento.Destino == StatusOs.AguardandoAprovacao
                ? await ObterOrcamentoVigente(evento.OsId, cancellationToken)
                : null;

            await notificador.NotificarMudancaDeStatus(
                new NotificacaoDeStatus(cliente.Email.Endereco, evento.Numero, evento.Origem, evento.Destino, evento.OcorridoEm, orcamento),
                cancellationToken);
        }
        catch (Exception falha) when (falha is not OperationCanceledException)
        {
            LogarFalha(logger, falha, evento.Numero, evento.Destino);
        }
    }

    private async Task<OrcamentoDaNotificacao> ObterOrcamentoVigente(Guid osId, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        var versao = os.VersaoVigente
            ?? throw new ConflitoDeEstadoException("A OS Aguardando aprovacao nao tem Versao do orcamento.");
        var itens = versao.Itens
            .Select(i => new ItemDoOrcamentoDaNotificacao(i.Descricao, i.Quantidade, i.PrecoUnitario, i.Subtotal))
            .ToList();
        return new OrcamentoDaNotificacao(itens, versao.Total, versao.Token);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao notificar o Cliente da OS {NumeroOs} sobre a mudanca para {Destino}")]
    private static partial void LogarFalha(ILogger logger, Exception falha, string numeroOs, StatusOs destino);
}
