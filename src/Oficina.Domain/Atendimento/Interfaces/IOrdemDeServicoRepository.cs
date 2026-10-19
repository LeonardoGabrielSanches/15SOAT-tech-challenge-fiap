using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.Domain.Atendimento.Interfaces;

public sealed record TempoDeExecucaoDaOs(IReadOnlyCollection<Guid> ServicoIds, TimeSpan Tempo);

public interface IOrdemDeServicoRepository
{
    Task<OrdemDeServico?> ObterPorId(Guid id, CancellationToken cancellationToken);

    Task<OrdemDeServico?> ObterPorNumero(string numero, CancellationToken cancellationToken);

    Task<OrdemDeServico?> ObterPorTokenDoOrcamento(string token, CancellationToken cancellationToken);

    Task<bool> ExisteEmAndamentoDoVeiculo(Guid veiculoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrdemDeServico>> Listar(StatusOs? status, CancellationToken cancellationToken);

    Task<IReadOnlyList<TempoDeExecucaoDaOs>> ObterTemposDeExecucao(CancellationToken cancellationToken);

    void Adicionar(OrdemDeServico ordemDeServico);
}
