namespace Oficina.Domain.Common;

public abstract class RaizDeAgregado
{
    private readonly List<IEventoDeDominio> _eventos = [];

    public IReadOnlyList<IEventoDeDominio> EventosDeDominio => _eventos;

    protected void Registrar(IEventoDeDominio evento) => _eventos.Add(evento);

    public void LimparEventos() => _eventos.Clear();
}
