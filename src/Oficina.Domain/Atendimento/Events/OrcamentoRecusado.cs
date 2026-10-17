using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record OrcamentoRecusado(Guid OsId, string Numero, int Versao, string? Motivo, DateTimeOffset OcorridoEm) : IEventoDeDominio;
