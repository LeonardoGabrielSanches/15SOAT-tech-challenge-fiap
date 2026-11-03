using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;

namespace Oficina.Application.Estoque;

public sealed record PecaInsumoDto(
    Guid Id,
    string Nome,
    TipoPecaInsumo Tipo,
    string UnidadeDeMedida,
    decimal Preco,
    decimal Saldo,
    bool Ativo)
{
    public static PecaInsumoDto De(PecaInsumo item) =>
        new(item.Id, item.Nome, item.Tipo, item.UnidadeDeMedida, item.Preco.Valor, item.Saldo, item.Ativo);
}
