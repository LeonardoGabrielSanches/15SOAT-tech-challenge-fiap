using Oficina.Domain.Estoque.Entities;

namespace Oficina.Application.Estoque;

public sealed record SaldoDto(Guid Id, string Nome, string UnidadeDeMedida, decimal Saldo)
{
    public static SaldoDto De(PecaInsumo item) => new(item.Id, item.Nome, item.UnidadeDeMedida, item.Saldo);
}
