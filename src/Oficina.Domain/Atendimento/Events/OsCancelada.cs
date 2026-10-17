using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record OsCancelada(Guid OsId, string Numero, string Motivo, DateTimeOffset OcorridoEm) : IEventoDeDominio;
