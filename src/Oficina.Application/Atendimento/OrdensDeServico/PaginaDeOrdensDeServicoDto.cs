namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed record PaginaDeOrdensDeServicoDto(
    IReadOnlyList<OrdemDeServicoResumoDto> Itens,
    int Pagina,
    int Tamanho,
    int Total);
