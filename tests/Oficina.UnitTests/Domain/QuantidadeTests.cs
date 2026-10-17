using System.Globalization;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;

namespace Oficina.UnitTests.Domain;

public class QuantidadeTests
{
    private static decimal Numero(string valor) => decimal.Parse(valor, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(TipoPecaInsumo.Peca, "1")]
    [InlineData(TipoPecaInsumo.Peca, "4")]
    [InlineData(TipoPecaInsumo.Peca, "2.0")]
    [InlineData(TipoPecaInsumo.Insumo, "1")]
    [InlineData(TipoPecaInsumo.Insumo, "4.5")]
    [InlineData(TipoPecaInsumo.Insumo, "0.25")]
    public void Aceita_valor_compativel_com_o_tipo(TipoPecaInsumo tipo, string valor)
    {
        var quantidade = Quantidade.Criar(Numero(valor), tipo);

        Assert.Equal(Numero(valor), quantidade.Valor);
        Assert.Equal(tipo, quantidade.Tipo);
    }

    [Theory]
    [InlineData(TipoPecaInsumo.Peca, "1.5")]
    [InlineData(TipoPecaInsumo.Peca, "0.5")]
    [InlineData(TipoPecaInsumo.Peca, "0")]
    [InlineData(TipoPecaInsumo.Peca, "-1")]
    [InlineData(TipoPecaInsumo.Insumo, "0")]
    [InlineData(TipoPecaInsumo.Insumo, "-0.5")]
    [InlineData(TipoPecaInsumo.Insumo, "0.0001")]
    public void Rejeita_valor_incompativel_com_o_tipo(TipoPecaInsumo tipo, string valor)
    {
        Assert.Throws<DadosInvalidosException>(() => Quantidade.Criar(Numero(valor), tipo));
    }
}
