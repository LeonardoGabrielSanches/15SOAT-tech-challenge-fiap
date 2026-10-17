using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.Domain.Atendimento;

public class NumeroOsTests
{
    [Theory]
    [InlineData(2026, 1, "OS-2026-00001")]
    [InlineData(2026, 12345, "OS-2026-12345")]
    [InlineData(2027, 99999, "OS-2027-99999")]
    public void Formata_ano_e_sequencia(int ano, int sequencia, string esperado) =>
        Assert.Equal(esperado, NumeroOs.Criar(ano, sequencia).Valor);

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 100000)]
    [InlineData(999, 1)]
    public void Rejeita_valores_fora_do_formato(int ano, int sequencia) =>
        Assert.Throws<DadosInvalidosException>(() => NumeroOs.Criar(ano, sequencia));
}
