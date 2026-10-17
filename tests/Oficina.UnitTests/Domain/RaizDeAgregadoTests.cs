using Oficina.Domain.Common;

namespace Oficina.UnitTests.Domain;

public class RaizDeAgregadoTests
{
    private sealed record EventoFalso(DateTimeOffset OcorridoEm) : IEventoDeDominio;

    private sealed class AgregadoFalso : RaizDeAgregado
    {
        public void Fazer(IEventoDeDominio evento) => Registrar(evento);
    }

    [Fact]
    public void Eventos_registrados_ficam_disponiveis_ate_serem_limpos()
    {
        var agregado = new AgregadoFalso();
        var evento = new EventoFalso(DateTimeOffset.UnixEpoch);

        agregado.Fazer(evento);

        Assert.Equal(new IEventoDeDominio[] { evento }, agregado.EventosDeDominio);

        agregado.LimparEventos();

        Assert.Empty(agregado.EventosDeDominio);
    }

    [Theory]
    [InlineData(typeof(RecursoNaoEncontradoException))]
    [InlineData(typeof(ConflitoDeEstadoException))]
    [InlineData(typeof(DadosInvalidosException))]
    public void Excecoes_de_dominio_carregam_a_mensagem(Type tipo)
    {
        var excecao = (ExcecaoDeDominio)Activator.CreateInstance(tipo, "mensagem")!;

        Assert.Equal("mensagem", excecao.Message);
    }
}
