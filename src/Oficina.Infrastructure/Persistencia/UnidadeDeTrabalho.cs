using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Oficina.Application.Common;
using Oficina.Domain.Common;

namespace Oficina.Infrastructure.Persistencia;

public sealed class UnidadeDeTrabalho(DbContext contexto, IDespachanteDeEventos despachante) : IUnidadeDeTrabalho
{
    public async Task<IAsyncDisposable> IniciarTransacao(CancellationToken cancellationToken) =>
        new TransacaoDesfeitaSemConfirmacao(contexto.Database, await contexto.Database.BeginTransactionAsync(cancellationToken));

    public async Task Confirmar(CancellationToken cancellationToken)
    {
        var agregados = contexto.ChangeTracker
            .Entries<RaizDeAgregado>()
            .Select(e => e.Entity)
            .Where(a => a.EventosDeDominio.Count > 0)
            .ToList();
        var eventos = agregados.SelectMany(a => a.EventosDeDominio).ToList();

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflitoDeEstadoException("O registro foi alterado por outra operação. Tente novamente.");
        }

        await ConfirmarTransacaoAberta(cancellationToken);

        foreach (var agregado in agregados)
        {
            agregado.LimparEventos();
        }

        await despachante.Despachar(eventos, cancellationToken);
    }

    private async Task ConfirmarTransacaoAberta(CancellationToken cancellationToken)
    {
        if (contexto.Database.CurrentTransaction is not { } transacao)
        {
            return;
        }

        await transacao.CommitAsync(cancellationToken);
        await transacao.DisposeAsync();
    }

    private sealed class TransacaoDesfeitaSemConfirmacao(DatabaseFacade banco, IDbContextTransaction transacao) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            if (banco.CurrentTransaction == transacao)
            {
                await transacao.RollbackAsync();
            }

            await transacao.DisposeAsync();
        }
    }
}
