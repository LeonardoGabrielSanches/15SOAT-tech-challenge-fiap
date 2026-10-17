using Oficina.Domain.Common;

namespace Oficina.Application.Common;

public interface IManipuladorDeEvento<in TEvento> where TEvento : IEventoDeDominio
{
    Task Manipular(TEvento evento, CancellationToken cancellationToken);
}
