using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record OsAberta(Guid OsId, string Numero, Guid ClienteId, Guid VeiculoId, DateTimeOffset OcorridoEm) : IEventoDeDominio;
