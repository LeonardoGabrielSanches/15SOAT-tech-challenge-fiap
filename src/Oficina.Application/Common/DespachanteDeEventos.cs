using Oficina.Domain.Common;

namespace Oficina.Application.Common;

public sealed class DespachanteDeEventos(IServiceProvider serviceProvider) : IDespachanteDeEventos
{
    public async Task Despachar(IEnumerable<IEventoDeDominio> eventos, CancellationToken cancellationToken)
    {
        foreach (var evento in eventos)
        {
            var tipoManipulador = typeof(IManipuladorDeEvento<>).MakeGenericType(evento.GetType());
            var tipoColecao = typeof(IEnumerable<>).MakeGenericType(tipoManipulador);
            var manipuladores = (IEnumerable<object>?)serviceProvider.GetService(tipoColecao) ?? [];

            foreach (var manipulador in manipuladores)
            {
                var tarefa = (Task)tipoManipulador
                    .GetMethod(nameof(IManipuladorDeEvento<IEventoDeDominio>.Manipular))!
                    .Invoke(manipulador, [evento, cancellationToken])!;
                await tarefa;
            }
        }
    }
}
