using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.Application.Common;

public sealed record NotificacaoDeStatus(
    string Email,
    string NumeroOs,
    StatusOs? Origem,
    StatusOs Destino,
    DateTimeOffset Momento,
    OrcamentoDaNotificacao? Orcamento);
