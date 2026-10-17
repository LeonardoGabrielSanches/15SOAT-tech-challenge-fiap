using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.Domain.Atendimento.Interfaces;

public interface IOrdemDeServicoRepository
{
    Task<OrdemDeServico?> ObterPorId(Guid id, CancellationToken cancellationToken);

    Task<bool> ExisteEmAndamentoDoVeiculo(Guid veiculoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrdemDeServico>> Listar(StatusOs? status, CancellationToken cancellationToken);

    void Adicionar(OrdemDeServico ordemDeServico);
}
