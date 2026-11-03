using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Clientes;

public sealed class ConsultarCliente(IClienteRepository clientes)
{
    public async Task<ClienteDto> Executar(Guid id, CancellationToken cancellationToken) =>
        ClienteDto.De(await BuscaDeCliente.ObterOuFalhar(clientes, id, cancellationToken));
}
