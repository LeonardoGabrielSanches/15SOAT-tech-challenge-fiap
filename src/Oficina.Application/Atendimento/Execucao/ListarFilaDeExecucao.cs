using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Execucao;

public sealed class ListarFilaDeExecucao(IOrdemDeServicoRepository ordens)
{
    public async Task<IReadOnlyList<OrdemDeServicoResumoDto>> Executar(CancellationToken cancellationToken)
    {
        var aprovadas = await ordens.Listar(StatusOs.Aprovada, cancellationToken);

        return aprovadas
            .OrderBy(o => o.AbertaEm)
            .Select(OrdemDeServicoResumoDto.De)
            .ToList();
    }
}
