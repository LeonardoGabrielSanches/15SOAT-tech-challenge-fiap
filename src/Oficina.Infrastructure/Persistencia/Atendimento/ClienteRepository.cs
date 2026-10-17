using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Infrastructure.Persistencia.Atendimento;

public sealed class ClienteRepository(DbContext contexto) : IClienteRepository
{
    public Task<Cliente?> ObterPorId(Guid id, CancellationToken cancellationToken) =>
        contexto.Set<Cliente>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Cliente?> ObterPorDocumento(Documento documento, CancellationToken cancellationToken)
    {
        var numero = documento.Numero;
        return contexto.Set<Cliente>().FirstOrDefaultAsync(c => c.Documento.Numero == numero, cancellationToken);
    }

    public async Task<IReadOnlyList<Cliente>> Listar(bool? ativo, Documento? documento, CancellationToken cancellationToken)
    {
        var consulta = contexto.Set<Cliente>().AsQueryable();

        if (ativo is not null)
        {
            consulta = consulta.Where(c => c.Ativo == ativo);
        }

        if (documento is not null)
        {
            var numero = documento.Numero;
            consulta = consulta.Where(c => c.Documento.Numero == numero);
        }

        return await consulta.OrderBy(c => c.Nome).ThenBy(c => c.Id).ToListAsync(cancellationToken);
    }

    public void Adicionar(Cliente cliente) => contexto.Set<Cliente>().Add(cliente);
}
