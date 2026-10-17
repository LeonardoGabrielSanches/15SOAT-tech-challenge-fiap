using Oficina.Domain.CatalogoEstoque.Entities;

namespace Oficina.Domain.CatalogoEstoque.Interfaces;

public interface IServicoRepository
{
    void Adicionar(Servico servico);
    Task<Servico?> Obter(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Servico>> Listar(CancellationToken cancellationToken);
}
