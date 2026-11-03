using Microsoft.Extensions.Logging;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.Infrastructure;

public sealed partial class NotificadorSimulado(ILogger<NotificadorSimulado> logger) : INotificador
{
    public Task NotificarMudancaDeStatus(
        string email,
        string numeroOs,
        StatusOs? origem,
        StatusOs destino,
        DateTimeOffset momento,
        OrcamentoDaNotificacao? orcamento,
        CancellationToken cancellationToken)
    {
        LogarNotificacao(logger, email, numeroOs, origem, destino, momento, orcamento?.Total, orcamento?.Token);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Notificacao de status para {Email}: OS {NumeroOs} de {Origem} para {Destino} em {Momento}, orcamento {Total}, token {Token}")]
    private static partial void LogarNotificacao(
        ILogger logger,
        string email,
        string numeroOs,
        StatusOs? origem,
        StatusOs destino,
        DateTimeOffset momento,
        decimal? total,
        string? token);
}
