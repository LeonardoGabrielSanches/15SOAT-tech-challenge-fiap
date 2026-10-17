using Microsoft.Extensions.Logging;
using Oficina.Application.Common;

namespace Oficina.Infrastructure;

public sealed partial class NotificadorSimulado(ILogger<NotificadorSimulado> logger) : INotificador
{
    public Task EnviarOrcamento(string email, string numeroOs, decimal total, string token, CancellationToken cancellationToken)
    {
        LogarEnvio(logger, email, numeroOs, total, token);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Orcamento enviado para {Email}: OS {NumeroOs}, total {Total}, token {Token}")]
    private static partial void LogarEnvio(ILogger logger, string email, string numeroOs, decimal total, string token);
}
