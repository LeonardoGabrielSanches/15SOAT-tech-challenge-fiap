using Oficina.Application.Atendimento.Clientes;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Veiculos;

public sealed class ListarVeiculosDoCliente(IVeiculoRepository veiculos, IClienteRepository clientes)
{
    public async Task<IReadOnlyList<VeiculoDto>> Executar(Guid clienteId, bool? ativo, CancellationToken cancellationToken)
    {
        await BuscaDeCliente.ObterOuFalhar(clientes, clienteId, cancellationToken);
        var encontrados = await veiculos.ListarPorProprietario(clienteId, ativo, cancellationToken);
        return encontrados.Select(VeiculoDto.De).ToList();
    }
}
