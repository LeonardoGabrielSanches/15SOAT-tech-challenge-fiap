using Microsoft.EntityFrameworkCore;
using Moq;
using Oficina.Application.Common;
using Oficina.Domain.Common;
using Oficina.Infrastructure.Persistencia;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Infrastructure;

[Collection(PostgresCollection.Nome)]
public class UnidadeDeTrabalhoTests(PostgresFixture postgres)
{
    private sealed record EventoDeTeste(DateTimeOffset OcorridoEm) : IEventoDeDominio;

    private sealed class AgregadoDeTeste : RaizDeAgregado
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public void Ocorrer(IEventoDeDominio evento) => Registrar(evento);
    }

    private sealed class ContextoDeTeste(DbContextOptions<ContextoDeTeste> opcoes) : DbContext(opcoes)
    {
        public DbSet<AgregadoDeTeste> Agregados => Set<AgregadoDeTeste>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<AgregadoDeTeste>().Ignore(a => a.EventosDeDominio);
    }

    private ContextoDeTeste CriarContexto(string banco) =>
        new(new DbContextOptionsBuilder<ContextoDeTeste>().UseNpgsql(postgres.ConnectionString(banco)).Options);

    [Fact]
    public async Task Despacha_os_eventos_somente_depois_de_persistir()
    {
        const string banco = "oficina_uow";
        await using var contexto = CriarContexto(banco);
        await contexto.Database.EnsureCreatedAsync();
        var agregado = new AgregadoDeTeste();
        var evento = new EventoDeTeste(DateTimeOffset.UnixEpoch);
        agregado.Ocorrer(evento);
        contexto.Agregados.Add(agregado);
        var persistidoNoDespacho = false;
        var despachante = new Mock<IDespachanteDeEventos>();
        despachante
            .Setup(d => d.Despachar(It.IsAny<IEnumerable<IEventoDeDominio>>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                using var outro = CriarContexto(banco);
                persistidoNoDespacho = outro.Agregados.Any(a => a.Id == agregado.Id);
            })
            .Returns(Task.CompletedTask);
        var unidade = new UnidadeDeTrabalho(contexto, despachante.Object);

        await unidade.Confirmar(CancellationToken.None);

        Assert.True(persistidoNoDespacho);
        despachante.Verify(
            d => d.Despachar(It.Is<IEnumerable<IEventoDeDominio>>(e => e.SequenceEqual(new[] { evento })), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Empty(agregado.EventosDeDominio);
    }

    [Fact]
    public async Task Nao_despacha_quando_a_persistencia_falha()
    {
        const string banco = "oficina_uow_falha";
        await using var contexto = CriarContexto(banco);
        await contexto.Database.EnsureCreatedAsync();
        var repetido = new AgregadoDeTeste();
        contexto.Agregados.Add(repetido);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var duplicado = typeof(AgregadoDeTeste).GetProperty(nameof(AgregadoDeTeste.Id))!;
        var novo = new AgregadoDeTeste();
        duplicado.SetValue(novo, repetido.Id);
        novo.Ocorrer(new EventoDeTeste(DateTimeOffset.UnixEpoch));
        contexto.Agregados.Add(novo);
        var despachante = new Mock<IDespachanteDeEventos>();
        var unidade = new UnidadeDeTrabalho(contexto, despachante.Object);

        await Assert.ThrowsAsync<DbUpdateException>(() => unidade.Confirmar(CancellationToken.None));

        despachante.Verify(
            d => d.Despachar(It.IsAny<IEnumerable<IEventoDeDominio>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
