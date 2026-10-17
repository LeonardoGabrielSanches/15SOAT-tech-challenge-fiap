using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.Domain.Atendimento;

public class CpfTests
{
    [Theory]
    [InlineData("52998224725", "52998224725")]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData(" 529 982 247 25 ", "52998224725")]
    [InlineData("111.444.777-35", "11144477735")]
    [InlineData("000.000.001-91", "00000000191")]
    public void Aceita_cpf_valido_com_ou_sem_mascara(string entrada, string esperado)
    {
        Assert.Equal(esperado, Cpf.Criar(entrada).Numero);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("52998224724")]
    [InlineData("52998224715")]
    [InlineData("5299822472")]
    [InlineData("529982247250")]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("5299822472A")]
    public void Rejeita_cpf_invalido(string entrada)
    {
        Assert.Throws<DadosInvalidosException>(() => Cpf.Criar(entrada));
    }
}
