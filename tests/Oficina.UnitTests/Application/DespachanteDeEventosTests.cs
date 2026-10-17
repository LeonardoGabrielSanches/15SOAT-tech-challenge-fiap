using Moq;
using Oficina.Application.Common;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.Application;

public class DespachanteDeEventosTests
{
    public sealed record EventoFalso(DateTimeOffset OcorridoEm) : IEventoDeDominio;

    public sealed record OutroEvento(DateTimeOffset OcorridoEm) : IEventoDeDominio;

    [Fact]
    public async Task Despacha_o_evento_para_todos_os_manipuladores_do_seu_tipo()
    {
        var evento = new EventoFalso(DateTimeOffset.UnixEpoch);
        var primeiro = new Mock<IManipuladorDeEvento<EventoFalso>>();
        var segundo = new Mock<IManipuladorDeEvento<EventoFalso>>();
        var provider = new Mock<IServiceProvider>();
        provider
            .Setup(p => p.GetService(typeof(IEnumerable<IManipuladorDeEvento<EventoFalso>>)))
            .Returns(new[] { primeiro.Object, segundo.Object });
        var despachante = new DespachanteDeEventos(provider.Object);

        await despachante.Despachar([evento], CancellationToken.None);

        primeiro.Verify(m => m.Manipular(evento, It.IsAny<CancellationToken>()), Times.Once);
        segundo.Verify(m => m.Manipular(evento, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Evento_sem_manipulador_e_ignorado()
    {
        var provider = new Mock<IServiceProvider>();
        provider
            .Setup(p => p.GetService(typeof(IEnumerable<IManipuladorDeEvento<OutroEvento>>)))
            .Returns(Array.Empty<IManipuladorDeEvento<OutroEvento>>());
        var despachante = new DespachanteDeEventos(provider.Object);

        await despachante.Despachar([new OutroEvento(DateTimeOffset.UnixEpoch)], CancellationToken.None);
    }
}
