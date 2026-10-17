using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.CatalogoEstoque;

public class DinheiroTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(150.5)]
    public void Aceita_valores_nao_negativos(decimal valor)
    {
        Assert.Equal(valor, Dinheiro.De(valor).Valor);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-10)]
    public void Rejeita_valores_negativos(decimal valor)
    {
        Assert.Throws<DadosInvalidosException>(() => Dinheiro.De(valor));
    }

    [Fact]
    public void Arredonda_para_centavos()
    {
        Assert.Equal(10.13m, Dinheiro.De(10.125m).Valor);
    }

    [Fact]
    public void Valores_iguais_sao_equivalentes()
    {
        Assert.Equal(Dinheiro.De(5m), Dinheiro.De(5.00m));
    }
}
