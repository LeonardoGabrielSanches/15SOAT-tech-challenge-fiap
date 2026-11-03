using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.Clientes;

internal static class BuscaDeCliente
{
    public static async Task<Cliente> ObterOuFalhar(IClienteRepository clientes, Guid id, CancellationToken cancellationToken) =>
        await clientes.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Cliente nao encontrado.");
}
