using Oficina.Domain.Estoque.Entities;

namespace Oficina.Domain.Estoque.Interfaces;

public interface IPecaInsumoRepository
{
    Task<PecaInsumo?> Obter(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PecaInsumo>> Listar(bool incluirInativos, CancellationToken cancellationToken);

    void Adicionar(PecaInsumo item);
}
