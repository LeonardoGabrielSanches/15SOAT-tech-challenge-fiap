using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.Domain.Atendimento;

public class DocumentoEEmailTests
{
    [Theory]
    [InlineData("529.982.247-25", TipoDocumento.Cpf, "52998224725")]
    [InlineData("11.222.333/0001-81", TipoDocumento.Cnpj, "11222333000181")]
    [InlineData("12.ABC.345/01DE-35", TipoDocumento.Cnpj, "12ABC34501DE35")]
    public void Documento_identifica_o_tipo_pelo_conteudo(string entrada, TipoDocumento tipo, string numero)
    {
        var documento = Documento.Criar(entrada);

        Assert.Equal(tipo, documento.Tipo);
        Assert.Equal(numero, documento.Numero);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("52998224724")]
    [InlineData("11222333000180")]
    [InlineData("1122233300018112")]
    public void Documento_rejeita_valor_invalido(string entrada)
    {
        Assert.Throws<DadosInvalidosException>(() => Documento.Criar(entrada));
    }

    [Theory]
    [InlineData("maria@oficina.com", "maria@oficina.com")]
    [InlineData("  Maria.Silva+os@Oficina.com.br ", "maria.silva+os@oficina.com.br")]
    public void Email_valido_e_normalizado(string entrada, string esperado)
    {
        Assert.Equal(esperado, Email.Criar(entrada).Endereco);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("semarroba.com")]
    [InlineData("@oficina.com")]
    [InlineData("maria@")]
    [InlineData("maria@oficina")]
    [InlineData("ma ria@oficina.com")]
    [InlineData("a@b@oficina.com")]
    public void Email_invalido_e_rejeitado(string entrada)
    {
        Assert.Throws<DadosInvalidosException>(() => Email.Criar(entrada));
    }
}
