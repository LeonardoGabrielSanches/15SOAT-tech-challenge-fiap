using Oficina.Domain.Atendimento.Entities.ValueObjects;

namespace Oficina.Domain.Atendimento.Interfaces;

public interface IGeradorNumeroOs
{
    Task<NumeroOs> Proximo(int ano, CancellationToken cancellationToken);
}
