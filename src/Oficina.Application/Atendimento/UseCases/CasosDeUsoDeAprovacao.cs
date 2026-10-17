using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.UseCases;

internal static class BuscaDeOsPorToken
{
    public const string AutorCliente = "Cliente";

    public static async Task<OrdemDeServico> ObterOuFalhar(IOrdemDeServicoRepository ordens, string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DadosInvalidosException("O token de aprovacao e obrigatorio.");
        }

        return await ordens.ObterPorTokenDoOrcamento(token, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Token de aprovacao nao encontrado.");
    }
}

public sealed class AprovarOrcamento(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(string? token, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOsPorToken.ObterOuFalhar(ordens, token, cancellationToken);

        os.AprovarOrcamento(token!, BuscaDeOsPorToken.AutorCliente, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed class RecusarOrcamento(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(string? token, string? motivo, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOsPorToken.ObterOuFalhar(ordens, token, cancellationToken);

        os.RecusarOrcamento(token!, motivo, BuscaDeOsPorToken.AutorCliente, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
