using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Infrastructure.Notificacao;

namespace Oficina.IntegrationTests.Infrastructure.Notificacao;

public class MensagemDeMudancaDeStatusTests
{
    private const string UrlDoPortal = "http://localhost:3000";

    [Fact]
    public void Assunto_traz_o_numero_da_os_e_o_status_legivel()
    {
        var mensagem = MensagemDeMudancaDeStatus.Compor("OS-2026-00001", StatusOs.EmDiagnostico, StatusOs.EmExecucao, null, UrlDoPortal);

        Assert.Equal("OS-2026-00001: Em execução", mensagem.Assunto);
    }

    [Fact]
    public void Abertura_avisa_que_a_os_foi_recebida()
    {
        var mensagem = MensagemDeMudancaDeStatus.Compor("OS-2026-00001", null, StatusOs.Recebida, null, UrlDoPortal);

        Assert.Contains("A OS OS-2026-00001 foi aberta e está Recebida.", mensagem.Corpo);
    }

    [Fact]
    public void Mudanca_de_status_informa_origem_e_destino_sem_link_quando_nao_ha_orcamento()
    {
        var mensagem = MensagemDeMudancaDeStatus.Compor("OS-2026-00001", StatusOs.Aprovada, StatusOs.EmExecucao, null, UrlDoPortal);

        Assert.Contains("O status da OS OS-2026-00001 mudou de Aprovada para Em execução.", mensagem.Corpo);
        Assert.DoesNotContain("/portal/consulta", mensagem.Corpo);
    }

    [Fact]
    public void Aguardando_aprovacao_lista_itens_total_e_link_da_consulta_publica()
    {
        var orcamento = new OrcamentoDaNotificacao(
            [
                new ItemDoOrcamentoDaNotificacao("Troca de óleo", 1m, 1250m, 1250m),
                new ItemDoOrcamentoDaNotificacao("Óleo 5W30", 3.5m, 42.9m, 150.15m)
            ],
            1400.15m,
            "tok-123");

        var mensagem = MensagemDeMudancaDeStatus.Compor(
            "OS-2026-00001", StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao, orcamento, "https://oficina.exemplo/");

        Assert.Contains("- Troca de óleo: 1 x R$ 1.250,00 = R$ 1.250,00", mensagem.Corpo);
        Assert.Contains("- Óleo 5W30: 3,5 x R$ 42,90 = R$ 150,15", mensagem.Corpo);
        Assert.Contains("Total: R$ 1.400,15", mensagem.Corpo);
        Assert.Contains("https://oficina.exemplo/portal/consulta?numero=OS-2026-00001&token=tok-123", mensagem.Corpo);
    }
}
