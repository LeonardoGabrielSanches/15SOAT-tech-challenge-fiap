using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.Domain.Atendimento;

public class PlacaEAnoTests
{
    private static readonly TimeProvider Relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("ABC1234", "ABC1234")]
    [InlineData("abc-1234", "ABC1234")]
    [InlineData("ABC1D23", "ABC1D23")]
    [InlineData("abc-1d23", "ABC1D23")]
    [InlineData("  aBc1d23 ", "ABC1D23")]
    public void Placa_valida_e_normalizada(string entrada, string esperada) =>
        Assert.Equal(esperada, Placa.Criar(entrada).Valor);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC123")]
    [InlineData("ABC12345")]
    [InlineData("AB11234")]
    [InlineData("ABCD234")]
    [InlineData("ABC1DD3")]
    [InlineData("ABC-12-34")]
    [InlineData("ABC1D2D")]
    [InlineData("1BC1234")]
    public void Placa_invalida_e_rejeitada(string? entrada) =>
        Assert.Throws<DadosInvalidosException>(() => Placa.Criar(entrada));

    [Theory]
    [InlineData(1900)]
    [InlineData(2000)]
    [InlineData(2026)]
    [InlineData(2027)]
    public void Ano_valido_e_aceito(int ano) =>
        Assert.Equal(ano, AnoFabricacao.Criar(ano, Relogio).Valor);

    [Theory]
    [InlineData(1899)]
    [InlineData(2028)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ano_fora_do_intervalo_e_rejeitado(int ano) =>
        Assert.Throws<DadosInvalidosException>(() => AnoFabricacao.Criar(ano, Relogio));
}
