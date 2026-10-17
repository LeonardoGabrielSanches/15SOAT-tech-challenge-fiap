using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Events;

public sealed record ItemIncluido(Guid OsId, Guid PecaInsumoId, decimal Quantidade, DateTimeOffset OcorridoEm) : IEventoDeDominio;
