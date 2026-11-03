using Oficina.Domain.Atendimento.Entities;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed record OrdemDeServicoResumoDto(
    Guid Id,
    string Numero,
    string Status,
    Guid ClienteId,
    Guid VeiculoId,
    DateTimeOffset AbertaEm)
{
    public static OrdemDeServicoResumoDto De(OrdemDeServico os) =>
        new(os.Id, os.Numero.Valor, os.Status.ToString(), os.ClienteId, os.VeiculoId, os.AbertaEm);
}
