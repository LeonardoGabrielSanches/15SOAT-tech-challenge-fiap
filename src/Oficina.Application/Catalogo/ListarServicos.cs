using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Application.Catalogo;

public sealed class ListarServicos(IServicoRepository repositorio)
{
    public async Task<IReadOnlyList<ServicoDto>> Executar(CancellationToken cancellationToken) =>
        (await repositorio.Listar(cancellationToken)).Select(ServicoDto.De).ToList();
}
