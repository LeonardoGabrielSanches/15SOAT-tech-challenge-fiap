using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Infrastructure.Persistencia.Estoque;

public sealed class PecaInsumoRepository(DbContext contexto) : IPecaInsumoRepository
{
    public Task<PecaInsumo?> Obter(Guid id, CancellationToken cancellationToken) =>
        contexto.Set<PecaInsumo>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PecaInsumo>> Listar(bool incluirInativos, CancellationToken cancellationToken) =>
        await contexto.Set<PecaInsumo>()
            .Where(p => incluirInativos || p.Ativo)
            .OrderBy(p => p.Nome)
            .ToListAsync(cancellationToken);

    public void Adicionar(PecaInsumo item) => contexto.Set<PecaInsumo>().Add(item);
}
