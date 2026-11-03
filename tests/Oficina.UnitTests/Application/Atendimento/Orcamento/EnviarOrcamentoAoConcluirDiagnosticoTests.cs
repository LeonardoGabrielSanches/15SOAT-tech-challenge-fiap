using Moq;
using Oficina.Application.Atendimento.Orcamento;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.UnitTests.Application.Atendimento.Orcamento;

public class EnviarOrcamentoAoConcluirDiagnosticoTests
{
    [Fact]
    public async Task Handler_envia_o_orcamento_ao_email_do_cliente_pelo_notificador()
    {
        var cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
        var clientes = new Mock<IClienteRepository>();
        clientes.Setup(r => r.ObterPorId(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var notificador = new Mock<INotificador>();
        var evento = new DiagnosticoConcluido(Guid.NewGuid(), "OS-2026-00001", cliente.Id, 1, 175m, "TOKEN123", new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

        await new EnviarOrcamentoAoConcluirDiagnostico(clientes.Object, notificador.Object).Manipular(evento, CancellationToken.None);

        notificador.Verify(n => n.EnviarOrcamento("maria@oficina.com", "OS-2026-00001", 175m, "TOKEN123", It.IsAny<CancellationToken>()), Times.Once);
    }
}
