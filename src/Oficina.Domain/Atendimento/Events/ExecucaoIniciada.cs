using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record ExecucaoIniciada(Guid OsId, string Numero, DateTimeOffset OcorridoEm) : IEventoDeDominio;
