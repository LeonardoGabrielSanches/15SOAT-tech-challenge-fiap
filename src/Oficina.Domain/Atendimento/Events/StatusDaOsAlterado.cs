using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record StatusDaOsAlterado(
    Guid OsId,
    string Numero,
    Guid ClienteId,
    StatusOs? Origem,
    StatusOs Destino,
    DateTimeOffset OcorridoEm) : IEventoDeDominio;
