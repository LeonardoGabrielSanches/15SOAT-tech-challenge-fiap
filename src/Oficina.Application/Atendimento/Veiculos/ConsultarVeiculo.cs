using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Veiculos;

public sealed class ConsultarVeiculo(IVeiculoRepository veiculos)
{
    public async Task<VeiculoDto> Executar(Guid id, CancellationToken cancellationToken) =>
        VeiculoDto.De(await BuscaDeVeiculo.ObterOuFalhar(veiculos, id, cancellationToken));
}
