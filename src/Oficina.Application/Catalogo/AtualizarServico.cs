using Oficina.Application.Common;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Application.Catalogo;

public sealed class AtualizarServico(IServicoRepository repositorio, IUnidadeDeTrabalho unidade)
{
    public async Task<ServicoDto> Executar(Guid id, DadosDoServico dados, CancellationToken cancellationToken)
    {
        var servico = await BuscaDeServico.ObterOuFalhar(repositorio, id, cancellationToken);
        servico.Atualizar(dados.Nome, dados.Descricao, Dinheiro.De(dados.Preco));
        await unidade.Confirmar(cancellationToken);
        return ServicoDto.De(servico);
    }
}
