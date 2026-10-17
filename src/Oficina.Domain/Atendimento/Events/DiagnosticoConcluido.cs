using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record DiagnosticoConcluido(
    Guid OsId,
    string Numero,
    Guid ClienteId,
    int Versao,
    decimal Total,
    string Token,
    DateTimeOffset OcorridoEm) : IEventoDeDominio;
