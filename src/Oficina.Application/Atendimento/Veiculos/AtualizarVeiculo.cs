using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Veiculos;

public sealed record AtualizarVeiculoComando(Guid Id, string? Marca, string? Modelo, int AnoFabricacao);

public sealed class AtualizarVeiculo(IVeiculoRepository veiculos, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task<VeiculoDto> Executar(AtualizarVeiculoComando comando, CancellationToken cancellationToken)
    {
        var veiculo = await BuscaDeVeiculo.ObterOuFalhar(veiculos, comando.Id, cancellationToken);

        veiculo.AtualizarDados(comando.Marca, comando.Modelo, AnoFabricacao.Criar(comando.AnoFabricacao, relogio));

        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return VeiculoDto.De(veiculo);
    }
}
