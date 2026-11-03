using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public interface IConsultaDeOrdensDeServico
{
    Task<PaginaDeOrdensDeServicoDto> Listar(StatusOs? status, int pagina, int tamanho, CancellationToken cancellationToken);

    Task<IReadOnlyList<OrdemDeServicoResumoDto>> ListarPorStatus(StatusOs status, CancellationToken cancellationToken);

    Task<IReadOnlyList<TempoDeExecucaoDaOs>> ObterTemposDeExecucao(CancellationToken cancellationToken);
}
