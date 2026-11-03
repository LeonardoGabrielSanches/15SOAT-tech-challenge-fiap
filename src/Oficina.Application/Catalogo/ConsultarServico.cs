using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Application.Catalogo;

public sealed class ConsultarServico(IServicoRepository repositorio)
{
    public async Task<ServicoDto> Executar(Guid id, CancellationToken cancellationToken) =>
        ServicoDto.De(await BuscaDeServico.ObterOuFalhar(repositorio, id, cancellationToken));
}
