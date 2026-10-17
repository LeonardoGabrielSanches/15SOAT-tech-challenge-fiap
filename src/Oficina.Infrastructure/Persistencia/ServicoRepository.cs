using Microsoft.EntityFrameworkCore;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Infrastructure.Persistencia;

public sealed class ServicoRepository(OficinaDbContext contexto) : IServicoRepository
{
    public void Adicionar(Servico servico) => contexto.Set<Servico>().Add(servico);

    public Task<Servico?> Obter(Guid id, CancellationToken cancellationToken) =>
        contexto.Set<Servico>().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Servico>> Listar(CancellationToken cancellationToken) =>
        await contexto.Set<Servico>().OrderBy(s => s.Nome).ToListAsync(cancellationToken);
}
