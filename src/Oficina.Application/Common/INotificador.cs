using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.Application.Common;

public interface INotificador
{
    Task NotificarMudancaDeStatus(
        string email,
        string numeroOs,
        StatusOs? origem,
        StatusOs destino,
        DateTimeOffset momento,
        OrcamentoDaNotificacao? orcamento,
        CancellationToken cancellationToken);
}
