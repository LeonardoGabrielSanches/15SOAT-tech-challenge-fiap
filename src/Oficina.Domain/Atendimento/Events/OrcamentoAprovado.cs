using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record OrcamentoAprovado(Guid OsId, string Numero, int Versao, DateTimeOffset OcorridoEm) : IEventoDeDominio;
