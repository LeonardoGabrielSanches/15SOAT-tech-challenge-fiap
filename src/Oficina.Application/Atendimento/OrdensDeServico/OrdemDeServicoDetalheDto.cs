using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.Veiculos;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed record ServicoDaOsDto(Guid ServicoId, string Nome, decimal Preco);

public sealed record ItemDePecaInsumoDaOsDto(Guid PecaInsumoId, string Nome, string UnidadeDeMedida, decimal Quantidade, decimal Preco);

public sealed record ItemDaVersaoDto(string Tipo, Guid ReferenciaId, string Descricao, decimal Quantidade, decimal PrecoUnitario, decimal Subtotal);

public sealed record VersaoDoOrcamentoDto(
    int Numero,
    decimal Total,
    string Situacao,
    DateTimeOffset GeradaEm,
    string? MotivoDaRecusa,
    IReadOnlyList<ItemDaVersaoDto> Itens);

public sealed record EntradaDoHistoricoDto(string? Origem, string Destino, DateTimeOffset Momento, string Autor);

public sealed record OrdemDeServicoDetalheDto(
    Guid Id,
    string Numero,
    string Status,
    DateTimeOffset AbertaEm,
    ClienteDto Cliente,
    VeiculoDto Veiculo,
    IReadOnlyList<ServicoDaOsDto> Servicos,
    IReadOnlyList<VersaoDoOrcamentoDto> VersoesDoOrcamento,
    IReadOnlyList<EntradaDoHistoricoDto> Historico,
    IReadOnlyList<ItemDePecaInsumoDaOsDto> PecasInsumos,
    string? MotivoDaUltimaRecusa);
