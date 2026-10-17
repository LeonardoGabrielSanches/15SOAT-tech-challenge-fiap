namespace Oficina.Domain.Estoque;

public interface IRepositorioDePecaInsumo
{
    Task<PecaInsumo?> Obter(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PecaInsumo>> Listar(bool incluirInativos, CancellationToken cancellationToken);

    void Adicionar(PecaInsumo item);
}
