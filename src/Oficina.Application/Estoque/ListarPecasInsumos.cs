using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Estoque;

public sealed class ListarPecasInsumos(IPecaInsumoRepository repositorio)
{
    public async Task<IReadOnlyList<PecaInsumoDto>> Executar(bool incluirInativos, CancellationToken cancellationToken) =>
        (await repositorio.Listar(incluirInativos, cancellationToken)).Select(PecaInsumoDto.De).ToList();
}
