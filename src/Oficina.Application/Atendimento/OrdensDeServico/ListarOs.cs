using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed class ListarOs(IOrdemDeServicoRepository ordens)
{
    private static readonly StatusOs[] OrdemDePrioridade =
    [
        StatusOs.EmExecucao,
        StatusOs.Aprovada,
        StatusOs.AguardandoAprovacao,
        StatusOs.EmDiagnostico,
        StatusOs.Recebida,
        StatusOs.Finalizada
    ];

    public async Task<IReadOnlyList<OrdemDeServicoResumoDto>> Executar(StatusOs? status, CancellationToken cancellationToken)
    {
        var encontradas = await ordens.Listar(status, cancellationToken);

        return encontradas
            .OrderBy(o => Array.IndexOf(OrdemDePrioridade, o.Status) is var i and >= 0 ? i : OrdemDePrioridade.Length)
            .ThenBy(o => o.AbertaEm)
            .Select(OrdemDeServicoResumoDto.De)
            .ToList();
    }
}
