using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Clientes;

public sealed class InativarCliente(IClienteRepository clientes, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await BuscaDeCliente.ObterOuFalhar(clientes, id, cancellationToken);

        cliente.Inativar();

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
