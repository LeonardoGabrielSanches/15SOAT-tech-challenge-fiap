namespace Oficina.Application.Common;

public interface INotificador
{
    Task EnviarOrcamento(string email, string numeroOs, decimal total, string token, CancellationToken cancellationToken);
}
