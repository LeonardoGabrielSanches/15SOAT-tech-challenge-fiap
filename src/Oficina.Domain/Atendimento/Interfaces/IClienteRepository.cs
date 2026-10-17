using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;

namespace Oficina.Domain.Atendimento.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorId(Guid id, CancellationToken cancellationToken);

    Task<Cliente?> ObterPorDocumento(Documento documento, CancellationToken cancellationToken);

    Task<IReadOnlyList<Cliente>> Listar(bool? ativo, Documento? documento, CancellationToken cancellationToken);

    void Adicionar(Cliente cliente);
}
