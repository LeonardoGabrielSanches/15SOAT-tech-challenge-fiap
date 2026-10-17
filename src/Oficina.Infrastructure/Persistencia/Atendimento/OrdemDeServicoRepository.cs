using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Infrastructure.Persistencia.Atendimento;

public sealed class OrdemDeServicoRepository(DbContext contexto) : IOrdemDeServicoRepository
{
    public Task<OrdemDeServico?> ObterPorId(Guid id, CancellationToken cancellationToken) =>
        contexto.Set<OrdemDeServico>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<OrdemDeServico?> ObterPorNumero(string numero, CancellationToken cancellationToken) =>
        contexto.Set<OrdemDeServico>().FirstOrDefaultAsync(o => o.Numero == NumeroOs.Reconstituir(numero), cancellationToken);

    public Task<OrdemDeServico?> ObterPorTokenDoOrcamento(string token, CancellationToken cancellationToken) =>
        contexto.Set<OrdemDeServico>().FirstOrDefaultAsync(o => o.Versoes.Any(v => v.Token == token), cancellationToken);

    public Task<bool> ExisteEmAndamentoDoVeiculo(Guid veiculoId, CancellationToken cancellationToken) =>
        contexto.Set<OrdemDeServico>().AnyAsync(
            o => o.VeiculoId == veiculoId && o.Status != StatusOs.Entregue && o.Status != StatusOs.Cancelada,
            cancellationToken);

    public async Task<IReadOnlyList<OrdemDeServico>> Listar(StatusOs? status, CancellationToken cancellationToken)
    {
        var consulta = contexto.Set<OrdemDeServico>().AsQueryable();

        if (status is not null)
        {
            consulta = consulta.Where(o => o.Status == status);
        }

        return await consulta.ToListAsync(cancellationToken);
    }

    public void Adicionar(OrdemDeServico ordemDeServico) => contexto.Set<OrdemDeServico>().Add(ordemDeServico);
}
