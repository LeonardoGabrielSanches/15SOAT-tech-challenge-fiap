using Oficina.Application.Common;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Estoque;

public sealed record CadastrarPecaInsumoComando(string Nome, TipoPecaInsumo Tipo, string UnidadeDeMedida, decimal Preco);

public sealed class CadastrarPecaInsumo(IPecaInsumoRepository repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<PecaInsumoDto> Executar(CadastrarPecaInsumoComando comando, CancellationToken cancellationToken)
    {
        var item = PecaInsumo.Criar(comando.Nome, comando.Tipo, comando.UnidadeDeMedida, Dinheiro.De(comando.Preco));
        repositorio.Adicionar(item);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return PecaInsumoDto.De(item);
    }
}
