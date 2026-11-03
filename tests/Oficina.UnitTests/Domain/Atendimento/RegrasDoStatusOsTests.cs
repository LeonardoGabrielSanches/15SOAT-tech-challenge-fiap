using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.UnitTests.Domain.Atendimento;

public class RegrasDoStatusOsTests
{
    [Theory]
    [InlineData(StatusOs.Recebida, true)]
    [InlineData(StatusOs.EmDiagnostico, true)]
    [InlineData(StatusOs.AguardandoAprovacao, true)]
    [InlineData(StatusOs.Aprovada, true)]
    [InlineData(StatusOs.EmExecucao, true)]
    [InlineData(StatusOs.Finalizada, true)]
    [InlineData(StatusOs.Entregue, false)]
    [InlineData(StatusOs.Cancelada, false)]
    public void Os_em_andamento_e_a_que_nao_esta_entregue_nem_cancelada(StatusOs status, bool esperado) =>
        Assert.Equal(esperado, status.EstaEmAndamento());

    [Theory]
    [InlineData(StatusOs.Recebida, true)]
    [InlineData(StatusOs.EmDiagnostico, true)]
    [InlineData(StatusOs.AguardandoAprovacao, true)]
    [InlineData(StatusOs.Aprovada, true)]
    [InlineData(StatusOs.EmExecucao, true)]
    [InlineData(StatusOs.Finalizada, false)]
    [InlineData(StatusOs.Entregue, false)]
    [InlineData(StatusOs.Cancelada, false)]
    public void Listagem_padrao_omite_finalizada_entregue_e_cancelada(StatusOs status, bool esperado) =>
        Assert.Equal(esperado, status.ApareceNaListagemPadrao());

    [Fact]
    public void Prioridade_vai_do_trabalho_mais_urgente_ao_menos_urgente()
    {
        StatusOs[] daListagemPadrao =
            [StatusOs.Recebida, StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao, StatusOs.Aprovada, StatusOs.EmExecucao];

        Assert.Equal(
            [StatusOs.EmExecucao, StatusOs.Aprovada, StatusOs.AguardandoAprovacao, StatusOs.EmDiagnostico, StatusOs.Recebida],
            daListagemPadrao.OrderBy(s => s.PrioridadeNaListagem()));
    }

    [Theory]
    [InlineData(StatusOs.Finalizada)]
    [InlineData(StatusOs.Entregue)]
    [InlineData(StatusOs.Cancelada)]
    public void Status_fora_da_listagem_padrao_vem_depois_de_todos_os_da_listagem_padrao(StatusOs status) =>
        Assert.True(status.PrioridadeNaListagem() > StatusOs.Recebida.PrioridadeNaListagem());
}
