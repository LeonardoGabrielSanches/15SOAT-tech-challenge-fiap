namespace Oficina.Domain.Common;

public readonly record struct Dinheiro
{
    public Dinheiro(decimal valor)
    {
        if (valor < 0)
        {
            throw new DadosInvalidosException("O valor em dinheiro não pode ser negativo.");
        }

        if (decimal.Round(valor, 2) != valor)
        {
            throw new DadosInvalidosException("O valor em dinheiro aceita no máximo duas casas decimais.");
        }

        Valor = valor;
    }

    public decimal Valor { get; }
}
