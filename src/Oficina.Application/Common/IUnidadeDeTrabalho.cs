namespace Oficina.Application.Common;

public interface IUnidadeDeTrabalho
{
    Task<IAsyncDisposable> IniciarTransacao(CancellationToken cancellationToken);

    Task Confirmar(CancellationToken cancellationToken);
}
