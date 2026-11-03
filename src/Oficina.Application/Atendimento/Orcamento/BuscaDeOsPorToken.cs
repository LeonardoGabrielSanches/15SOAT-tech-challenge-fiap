using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.Orcamento;

internal static class BuscaDeOsPorToken
{
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
