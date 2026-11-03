namespace Oficina.Application.Common;

public interface IUnidadeDeTrabalho
{
    Task IniciarTransacao(CancellationToken cancellationToken);

    Task Confirmar(CancellationToken cancellationToken);
}
