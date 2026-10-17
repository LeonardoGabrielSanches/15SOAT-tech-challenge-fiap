using Oficina.Application.Common;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.CatalogoEstoque.UseCases;

public sealed record DadosDoServico(string Nome, string? Descricao, decimal Preco);

public sealed record ServicoDto(Guid Id, string Nome, string? Descricao, decimal Preco, bool Ativo)
{
    public static ServicoDto De(Servico servico) =>
        new(servico.Id, servico.Nome, servico.Descricao, servico.Preco.Valor, servico.Ativo);
}

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

public sealed class ConsultarServico(IServicoRepository repositorio)
{
    public async Task<ServicoDto> Executar(Guid id, CancellationToken cancellationToken) =>
        ServicoDto.De(await ServicoRepositoryExtensions.ObterOuFalhar(repositorio, id, cancellationToken));
}

public sealed class ListarServicos(IServicoRepository repositorio)
{
    public async Task<IReadOnlyList<ServicoDto>> Executar(CancellationToken cancellationToken) =>
        (await repositorio.Listar(cancellationToken)).Select(ServicoDto.De).ToList();
}

public sealed class AtualizarServico(IServicoRepository repositorio, IUnidadeDeTrabalho unidade)
{
    public async Task<ServicoDto> Executar(Guid id, DadosDoServico dados, CancellationToken cancellationToken)
    {
        var servico = await ServicoRepositoryExtensions.ObterOuFalhar(repositorio, id, cancellationToken);
        servico.Atualizar(dados.Nome, dados.Descricao, Dinheiro.De(dados.Preco));
        await unidade.Confirmar(cancellationToken);
        return ServicoDto.De(servico);
    }
}

public sealed class InativarServico(IServicoRepository repositorio, IUnidadeDeTrabalho unidade)
{
    public async Task Executar(Guid id, CancellationToken cancellationToken)
    {
        var servico = await ServicoRepositoryExtensions.ObterOuFalhar(repositorio, id, cancellationToken);
        servico.Inativar();
        await unidade.Confirmar(cancellationToken);
    }
}

internal static class ServicoRepositoryExtensions
{
    public static async Task<Servico> ObterOuFalhar(IServicoRepository repositorio, Guid id, CancellationToken cancellationToken) =>
        await repositorio.Obter(id, cancellationToken)
        ?? throw new RecursoNaoEncontradoException("Servico nao encontrado.");
}
