using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.Domain.Atendimento;

public class CnpjTests
{
    [Theory]
    [InlineData("11222333000181", "11222333000181")]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("11.444.777/0001-61", "11444777000161")]
    [InlineData("12ABC34501DE35", "12ABC34501DE35")]
    [InlineData("12.ABC.345/01DE-35", "12ABC34501DE35")]
    [InlineData("12.abc.345/01de-35", "12ABC34501DE35")]
    public void Aceita_cnpj_numerico_ou_alfanumerico_com_ou_sem_mascara(string entrada, string esperado)
    {
        Assert.Equal(esperado, Cnpj.Criar(entrada).Numero);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("11222333000180")]
    [InlineData("11222333000191")]
    [InlineData("12ABC34501DE34")]
    [InlineData("12ABC34501DE3A")]
    [InlineData("12ABC34501DE")]
    [InlineData("12ABC34501DE355")]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    [InlineData("12ABC34501D@35")]
    public void Rejeita_cnpj_invalido(string entrada)
    {
        Assert.Throws<DadosInvalidosException>(() => Cnpj.Criar(entrada));
    }
}
