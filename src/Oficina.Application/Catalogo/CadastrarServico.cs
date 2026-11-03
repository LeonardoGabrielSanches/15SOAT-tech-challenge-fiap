using Oficina.Application.Common;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Application.Catalogo;

public sealed class CadastrarServico(IServicoRepository repositorio, IUnidadeDeTrabalho unidade)
{
    public async Task<ServicoDto> Executar(DadosDoServico dados, CancellationToken cancellationToken)
    {
        var servico = Servico.Cadastrar(dados.Nome, dados.Descricao, Dinheiro.De(dados.Preco));
        repositorio.Adicionar(servico);
        await unidade.Confirmar(cancellationToken);
        return ServicoDto.De(servico);
    }
}
