using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common;
using Oficina.Domain.Common;

namespace Oficina.Infrastructure.Persistencia;

public sealed class UnidadeDeTrabalho(DbContext contexto, IDespachanteDeEventos despachante) : IUnidadeDeTrabalho
{
    public async Task Confirmar(CancellationToken cancellationToken)
    {
        var agregados = contexto.ChangeTracker
            .Entries<RaizDeAgregado>()
            .Select(e => e.Entity)
            .Where(a => a.EventosDeDominio.Count > 0)
            .ToList();
        var eventos = agregados.SelectMany(a => a.EventosDeDominio).ToList();

        await contexto.SaveChangesAsync(cancellationToken);

        foreach (var agregado in agregados)
        {
            agregado.LimparEventos();
        }

        await despachante.Despachar(eventos, cancellationToken);
    }
}
