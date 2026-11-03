using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Atendimento.OrdensDeServico;

internal static class PecasInsumosDaOs
{
    public static async Task<PecaInsumo> ObterOuFalhar(IPecaInsumoRepository pecas, Guid id, CancellationToken cancellationToken) =>
        await pecas.Obter(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Peca/insumo nao encontrada.");

    public static async Task<IReadOnlyList<PecaInsumo>> Carregar(IPecaInsumoRepository pecas, OrdemDeServico os, CancellationToken cancellationToken)
    {
        var carregadas = new List<PecaInsumo>();
        foreach (var item in os.ItensDePecaInsumo)
        {
            carregadas.Add(await ObterOuFalhar(pecas, item.PecaInsumoId, cancellationToken));
        }

        return carregadas;
    }
}
