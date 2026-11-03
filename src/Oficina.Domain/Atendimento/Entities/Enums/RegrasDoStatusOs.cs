namespace Oficina.Domain.Atendimento.Entities.Enums;

public static class RegrasDoStatusOs
{
    private static readonly StatusOs[] ListagemPadraoPorPrioridade =
    [
        StatusOs.EmExecucao,
        StatusOs.Aprovada,
        StatusOs.AguardandoAprovacao,
        StatusOs.EmDiagnostico,
        StatusOs.Recebida
    ];

    public static bool EstaEmAndamento(this StatusOs status) =>
        status is not (StatusOs.Entregue or StatusOs.Cancelada);

    public static bool ApareceNaListagemPadrao(this StatusOs status) =>
        ListagemPadraoPorPrioridade.Contains(status);

    public static int PrioridadeNaListagem(this StatusOs status) =>
        Array.IndexOf(ListagemPadraoPorPrioridade, status) is var posicao and >= 0
            ? posicao
            : ListagemPadraoPorPrioridade.Length;
}
