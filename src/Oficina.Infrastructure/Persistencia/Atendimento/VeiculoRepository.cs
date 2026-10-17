using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Infrastructure.Persistencia.Atendimento;

public sealed class VeiculoRepository(DbContext contexto) : IVeiculoRepository
{
    public Task<Veiculo?> ObterPorId(Guid id, CancellationToken cancellationToken) =>
        contexto.Set<Veiculo>().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Veiculo?> ObterPorPlaca(Placa placa, CancellationToken cancellationToken) =>
        contexto.Set<Veiculo>().FirstOrDefaultAsync(v => v.Placa == placa, cancellationToken);

    public async Task<IReadOnlyList<Veiculo>> ListarPorProprietario(Guid proprietarioId, bool? ativo, CancellationToken cancellationToken)
    {
        var consulta = contexto.Set<Veiculo>().Where(v => v.ProprietarioId == proprietarioId);

        if (ativo is not null)
        {
            consulta = consulta.Where(v => v.Ativo == ativo);
        }

        return await consulta.OrderBy(v => v.Placa).ThenBy(v => v.Id).ToListAsync(cancellationToken);
    }

    public void Adicionar(Veiculo veiculo) => contexto.Set<Veiculo>().Add(veiculo);
}
