namespace Oficina.Application.Common;

public sealed record OrcamentoDaNotificacao(IReadOnlyList<ItemDoOrcamentoDaNotificacao> Itens, decimal Total, string Token);

public sealed record ItemDoOrcamentoDaNotificacao(string Descricao, decimal Quantidade, decimal PrecoUnitario, decimal Subtotal);
