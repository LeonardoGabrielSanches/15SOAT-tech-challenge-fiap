using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Orcamento;

public sealed class EnviarOrcamentoAoConcluirDiagnostico(IClienteRepository clientes, INotificador notificador)
    : IManipuladorDeEvento<DiagnosticoConcluido>
{
    public async Task Manipular(DiagnosticoConcluido evento, CancellationToken cancellationToken)
    {
        var cliente = await BuscaDeCliente.ObterOuFalhar(clientes, evento.ClienteId, cancellationToken);

        await notificador.EnviarOrcamento(cliente.Email.Endereco, evento.Numero, evento.Total, evento.Token, cancellationToken);
    }
}
