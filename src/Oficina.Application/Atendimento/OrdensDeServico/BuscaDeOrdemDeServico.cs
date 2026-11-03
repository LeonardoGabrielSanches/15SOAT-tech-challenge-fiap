using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.OrdensDeServico;

internal static class BuscaDeOrdemDeServico
{
    public static async Task<OrdemDeServico> ObterOuFalhar(IOrdemDeServicoRepository ordens, Guid id, CancellationToken cancellationToken) =>
        await ordens.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("OS nao encontrada.");
}
