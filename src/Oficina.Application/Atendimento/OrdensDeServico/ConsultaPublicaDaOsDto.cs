namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed record ItemDoOrcamentoPublicoDto(string Descricao, decimal Quantidade, decimal PrecoUnitario, decimal Subtotal);

public sealed record OrcamentoPublicoDto(int Versao, string Situacao, IReadOnlyList<ItemDoOrcamentoPublicoDto> Itens, decimal Total);

public sealed record EntradaDoHistoricoPublicoDto(string? Origem, string Destino, DateTimeOffset Momento, string Autor);

public sealed record ConsultaPublicaDaOsDto(
    string Numero,
    string Status,
    DateTimeOffset AbertaEm,
    IReadOnlyList<EntradaDoHistoricoPublicoDto> Historico,
    OrcamentoPublicoDto? OrcamentoVigente);
