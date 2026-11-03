using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Estoque;

public sealed class ConsultarPecaInsumo(IPecaInsumoRepository repositorio)
{
    public async Task<PecaInsumoDto> Executar(Guid id, CancellationToken cancellationToken) =>
        PecaInsumoDto.De(await BuscaDePecaInsumo.ObterOuFalhar(repositorio, id, cancellationToken));
}
