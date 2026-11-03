using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Catalogo;

internal static class BuscaDeServico
{
    public static async Task<Servico> ObterOuFalhar(IServicoRepository servicos, Guid id, CancellationToken cancellationToken) =>
        await servicos.Obter(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Servico nao encontrado.");
}
