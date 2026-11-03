using Oficina.Application.Common;
using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Application.Catalogo;

public sealed class InativarServico(IServicoRepository repositorio, IUnidadeDeTrabalho unidade)
{
    public async Task Executar(Guid id, CancellationToken cancellationToken)
    {
        var servico = await BuscaDeServico.ObterOuFalhar(repositorio, id, cancellationToken);
        servico.Inativar();
        await unidade.Confirmar(cancellationToken);
    }
}
