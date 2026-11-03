using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Veiculos;

public sealed class InativarVeiculo(IVeiculoRepository veiculos, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid id, CancellationToken cancellationToken)
    {
        var veiculo = await BuscaDeVeiculo.ObterOuFalhar(veiculos, id, cancellationToken);

        veiculo.Inativar();

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
