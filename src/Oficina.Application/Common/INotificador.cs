namespace Oficina.Application.Common;

public interface INotificador
{
    Task NotificarMudancaDeStatus(NotificacaoDeStatus notificacao, CancellationToken cancellationToken);
}
