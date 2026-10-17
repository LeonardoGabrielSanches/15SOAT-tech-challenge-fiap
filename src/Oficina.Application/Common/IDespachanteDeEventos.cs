using Oficina.Domain.Common;

namespace Oficina.Application.Common;

public interface IDespachanteDeEventos
{
    Task Despachar(IEnumerable<IEventoDeDominio> eventos, CancellationToken cancellationToken);
}
