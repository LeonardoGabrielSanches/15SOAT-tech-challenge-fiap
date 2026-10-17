namespace Oficina.Application.Common;

public interface IUnidadeDeTrabalho
{
    Task Confirmar(CancellationToken cancellationToken);
}
