using System.Globalization;
using System.Text;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.Infrastructure.Notificacao;

public sealed record MensagemDeMudancaDeStatus(string Assunto, string Corpo)
{
    private static readonly NumberFormatInfo FormatoBrasileiro = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "."
    };

    public static MensagemDeMudancaDeStatus Compor(NotificacaoDeStatus notificacao, string urlDoPortal)
    {
        var corpo = new StringBuilder()
            .AppendLine("Olá,")
            .AppendLine()
            .AppendLine(MudancaDeStatus(notificacao));

        if (notificacao.Orcamento is not null)
        {
            AcrescentarOrcamento(corpo, notificacao.NumeroOs, notificacao.Orcamento, urlDoPortal);
        }

        corpo.AppendLine().AppendLine("Oficina");
        return new MensagemDeMudancaDeStatus($"{notificacao.NumeroOs}: {Rotulo(notificacao.Destino)}", corpo.ToString());
    }

    private static string MudancaDeStatus(NotificacaoDeStatus notificacao) => notificacao.Origem is null
        ? $"A OS {notificacao.NumeroOs} foi aberta e está {Rotulo(notificacao.Destino)}."
        : $"O status da OS {notificacao.NumeroOs} mudou de {Rotulo(notificacao.Origem.Value)} para {Rotulo(notificacao.Destino)}.";

    private static void AcrescentarOrcamento(StringBuilder corpo, string numeroOs, OrcamentoDaNotificacao orcamento, string urlDoPortal)
    {
        corpo.AppendLine().AppendLine("Orçamento:");
        foreach (var item in orcamento.Itens)
        {
            corpo.AppendLine($"- {item.Descricao}: {Quantidade(item.Quantidade)} x {Moeda(item.PrecoUnitario)} = {Moeda(item.Subtotal)}");
        }

        corpo.AppendLine($"Total: {Moeda(orcamento.Total)}")
            .AppendLine()
            .AppendLine("Para ver a OS e aprovar ou recusar o orçamento, acesse o link abaixo e informe o CPF/CNPJ do cadastro:")
            .AppendLine(LinkDaConsulta(urlDoPortal, numeroOs, orcamento.Token));
    }

    private static string LinkDaConsulta(string urlDoPortal, string numeroOs, string token) =>
        $"{urlDoPortal.TrimEnd('/')}/portal/consulta?numero={Uri.EscapeDataString(numeroOs)}&token={Uri.EscapeDataString(token)}";

    private static string Moeda(decimal valor) => $"R$ {valor.ToString("#,##0.00", FormatoBrasileiro)}";

    private static string Quantidade(decimal valor) => valor.ToString("#,##0.###", FormatoBrasileiro);

    private static string Rotulo(StatusOs status) => status switch
    {
        StatusOs.Recebida => "Recebida",
        StatusOs.EmDiagnostico => "Em diagnóstico",
        StatusOs.AguardandoAprovacao => "Aguardando aprovação",
        StatusOs.Aprovada => "Aprovada",
        StatusOs.EmExecucao => "Em execução",
        StatusOs.Finalizada => "Finalizada",
        StatusOs.Entregue => "Entregue",
        StatusOs.Cancelada => "Cancelada",
        _ => status.ToString()
    };
}
