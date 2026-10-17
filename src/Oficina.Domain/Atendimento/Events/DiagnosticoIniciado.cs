using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record DiagnosticoIniciado(Guid OsId, string Numero, DateTimeOffset OcorridoEm) : IEventoDeDominio;
