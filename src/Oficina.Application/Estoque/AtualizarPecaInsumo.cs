using Oficina.Application.Common;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Estoque;

public sealed record AtualizarPecaInsumoComando(string Nome, string UnidadeDeMedida, decimal Preco);

public sealed class AtualizarPecaInsumo(IPecaInsumoRepository repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<PecaInsumoDto> Executar(Guid id, AtualizarPecaInsumoComando comando, CancellationToken cancellationToken)
    {
        var item = await BuscaDePecaInsumo.ObterOuFalhar(repositorio, id, cancellationToken);
        item.Atualizar(comando.Nome, comando.UnidadeDeMedida, Dinheiro.De(comando.Preco));
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return PecaInsumoDto.De(item);
    }
}
