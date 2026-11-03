using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.Veiculos;

internal static class BuscaDeVeiculo
{
    public static async Task<Veiculo> ObterOuFalhar(IVeiculoRepository veiculos, Guid id, CancellationToken cancellationToken) =>
        await veiculos.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Veiculo nao encontrado.");
}
