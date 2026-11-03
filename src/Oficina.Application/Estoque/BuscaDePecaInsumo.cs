using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Estoque;

internal static class BuscaDePecaInsumo
{
    public static async Task<PecaInsumo> ObterOuFalhar(IPecaInsumoRepository repositorio, Guid id, CancellationToken cancellationToken) =>
        await repositorio.Obter(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Peça/insumo não encontrada.");
}
