using Oficina.Application.Common;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Estoque;

public sealed class InativarPecaInsumo(IPecaInsumoRepository repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid id, CancellationToken cancellationToken)
    {
        var item = await BuscaDePecaInsumo.ObterOuFalhar(repositorio, id, cancellationToken);
        item.Inativar();
        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
