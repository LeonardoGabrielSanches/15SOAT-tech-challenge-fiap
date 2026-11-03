using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.Application.Atendimento.Execucao;

public sealed class ListarFilaDeExecucao(IConsultaDeOrdensDeServico consulta)
{
    public Task<IReadOnlyList<OrdemDeServicoResumoDto>> Executar(CancellationToken cancellationToken) =>
        consulta.ListarPorStatus(StatusOs.Aprovada, cancellationToken);
}
