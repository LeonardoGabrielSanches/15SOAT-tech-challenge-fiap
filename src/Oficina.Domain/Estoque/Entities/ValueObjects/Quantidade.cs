using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities.Enums;

namespace Oficina.Domain.Estoque.Entities.ValueObjects;

public readonly record struct Quantidade
{
    public const int CasasDecimaisDoInsumo = 3;

    private Quantidade(decimal valor, TipoPecaInsumo tipo)
    {
        Valor = valor;
        Tipo = tipo;
    }

    public decimal Valor { get; }

    public TipoPecaInsumo Tipo { get; }

    public static Quantidade Criar(decimal valor, TipoPecaInsumo tipo)
    {
        if (valor <= 0)
        {
            throw new DadosInvalidosException("A quantidade deve ser positiva.");
        }

        var casasPermitidas = tipo == TipoPecaInsumo.Peca ? 0 : CasasDecimaisDoInsumo;
        if (decimal.Round(valor, casasPermitidas) != valor)
        {
            throw new DadosInvalidosException(tipo == TipoPecaInsumo.Peca
                ? "Uma peça aceita apenas quantidade inteira."
                : $"Um insumo aceita no máximo {CasasDecimaisDoInsumo} casas decimais.");
        }

        return new Quantidade(valor, tipo);
    }
}
