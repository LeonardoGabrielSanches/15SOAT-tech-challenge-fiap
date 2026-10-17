using Moq;

namespace Oficina.UnitTests.Domain.Atendimento;

internal static class RelogioFixo
{
    public static TimeProvider Em(DateTimeOffset instante)
    {
        var relogio = new Mock<TimeProvider>();
        relogio.Setup(r => r.GetUtcNow()).Returns(instante);
        return relogio.Object;
    }
}
