using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Estoque;

public sealed class ConsultarSaldo(IPecaInsumoRepository repositorio)
{
    public async Task<SaldoDto> Executar(Guid id, CancellationToken cancellationToken) =>
        SaldoDto.De(await BuscaDePecaInsumo.ObterOuFalhar(repositorio, id, cancellationToken));
}
