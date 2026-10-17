using Oficina.Domain.Common;

namespace Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;

public readonly record struct Dinheiro
{
    private Dinheiro(decimal valor) => Valor = valor;

    public decimal Valor { get; }

    public static Dinheiro De(decimal valor)
    {
        if (valor < 0)
        {
            throw new DadosInvalidosException("O valor em dinheiro nao pode ser negativo.");
        }

        return new Dinheiro(Math.Round(valor, 2, MidpointRounding.AwayFromZero));
    }
}
