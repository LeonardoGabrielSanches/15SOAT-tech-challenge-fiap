using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Veiculos;

public sealed record TransferirVeiculoComando(Guid VeiculoId, Guid NovoProprietarioId);

public sealed class TransferirVeiculo(IVeiculoRepository veiculos, IClienteRepository clientes, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<VeiculoDto> Executar(TransferirVeiculoComando comando, CancellationToken cancellationToken)
    {
        var veiculo = await BuscaDeVeiculo.ObterOuFalhar(veiculos, comando.VeiculoId, cancellationToken);
        var novoProprietario = await BuscaDeCliente.ObterOuFalhar(clientes, comando.NovoProprietarioId, cancellationToken);

        veiculo.Transferir(novoProprietario);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return VeiculoDto.De(veiculo);
    }
}
