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

    public static MensagemDeMudancaDeStatus Compor(
        string numeroOs,
        StatusOs? origem,
        StatusOs destino,
        OrcamentoDaNotificacao? orcamento,
        string urlDoPortal)
    {
        var corpo = new StringBuilder()
            .AppendLine("Olá,")
            .AppendLine()
            .AppendLine(Situacao(numeroOs, origem, destino));

        if (orcamento is not null)
        {
            AcrescentarOrcamento(corpo, numeroOs, orcamento, urlDoPortal);
        }

        corpo.AppendLine().AppendLine("Oficina");
        return new MensagemDeMudancaDeStatus($"{numeroOs}: {Rotulo(destino)}", corpo.ToString());
    }

    private static string Situacao(string numeroOs, StatusOs? origem, StatusOs destino) => origem is null
        ? $"A OS {numeroOs} foi aberta e está {Rotulo(destino)}."
        : $"O status da OS {numeroOs} mudou de {Rotulo(origem.Value)} para {Rotulo(destino)}.";

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
