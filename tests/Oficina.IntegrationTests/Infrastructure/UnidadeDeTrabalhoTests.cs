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

    [Fact]
    public async Task Descartar_a_transacao_sem_confirmar_desfaz_as_gravacoes_e_libera_os_bloqueios()
    {
        const string banco = "oficina_uow_desfazer";
        await using var contexto = CriarContexto(banco);
        await contexto.Database.EnsureCreatedAsync();
        var unidade = new UnidadeDeTrabalho(contexto, Mock.Of<IDespachanteDeEventos>());
        var id = Guid.NewGuid();

        await using (await unidade.IniciarTransacao(CancellationToken.None))
        {
            await contexto.Database.ExecuteSqlAsync($"""INSERT INTO "Agregados" ("Id") VALUES ({id})""");
        }

        Assert.Null(contexto.Database.CurrentTransaction);
        await using var outro = CriarContexto(banco);
        await outro.Database.OpenConnectionAsync();
        await outro.Database.ExecuteSqlRawAsync("SET lock_timeout = '2s'");
        await outro.Database.ExecuteSqlAsync($"""INSERT INTO "Agregados" ("Id") VALUES ({id})""");
        Assert.Equal(1, await outro.Agregados.CountAsync(a => a.Id == id));
    }

    [Fact]
    public async Task Descartar_depois_de_confirmar_mantem_o_que_foi_gravado()
    {
        const string banco = "oficina_uow_confirmada";
        await using var contexto = CriarContexto(banco);
        await contexto.Database.EnsureCreatedAsync();
        var unidade = new UnidadeDeTrabalho(contexto, Mock.Of<IDespachanteDeEventos>());
        var agregado = new AgregadoDeTeste();

        await using (await unidade.IniciarTransacao(CancellationToken.None))
        {
            contexto.Agregados.Add(agregado);
            await unidade.Confirmar(CancellationToken.None);
        }

        await using var outro = CriarContexto(banco);
        Assert.True(await outro.Agregados.AnyAsync(a => a.Id == agregado.Id));
    }
}
