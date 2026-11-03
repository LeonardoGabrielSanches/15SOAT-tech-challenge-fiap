using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Clientes;

public sealed record AtualizarClienteComando(Guid Id, string? Nome, string? Email, string? Telefone);

public sealed class AtualizarCliente(IClienteRepository clientes, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<ClienteDto> Executar(AtualizarClienteComando comando, CancellationToken cancellationToken)
    {
        var cliente = await BuscaDeCliente.ObterOuFalhar(clientes, comando.Id, cancellationToken);

        cliente.AtualizarDados(comando.Nome, Email.Criar(comando.Email), comando.Telefone);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return ClienteDto.De(cliente);
    }
}
