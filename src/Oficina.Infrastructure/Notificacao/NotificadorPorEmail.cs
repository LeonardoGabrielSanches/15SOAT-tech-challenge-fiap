using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;

namespace Oficina.Infrastructure.Notificacao;

public sealed class NotificadorPorEmail(IOptions<OpcoesDoEmail> opcoes) : INotificador
{
    public async Task NotificarMudancaDeStatus(
        string email,
        string numeroOs,
        StatusOs? origem,
        StatusOs destino,
        DateTimeOffset momento,
        OrcamentoDaNotificacao? orcamento,
        CancellationToken cancellationToken)
    {
        var configuracao = opcoes.Value;
        var conteudo = MensagemDeMudancaDeStatus.Compor(numeroOs, origem, destino, orcamento, configuracao.UrlDoPortal);
        var mensagem = new MimeMessage
        {
            Subject = conteudo.Assunto,
            Date = momento,
            Body = new TextPart("plain") { Text = conteudo.Corpo }
        };
        mensagem.From.Add(MailboxAddress.Parse(configuracao.Remetente));
        mensagem.To.Add(MailboxAddress.Parse(email));

        var limite = TimeSpan.FromSeconds(configuracao.TimeoutEmSegundos);
        using var prazo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        prazo.CancelAfter(limite);

        using var smtp = new SmtpClient { Timeout = (int)limite.TotalMilliseconds };
        try
        {
            await smtp.ConnectAsync(configuracao.Host, configuracao.Porta, configuracao.Tls, prazo.Token);
            if (!string.IsNullOrWhiteSpace(configuracao.Usuario))
            {
                await smtp.AuthenticateAsync(configuracao.Usuario, configuracao.Senha ?? string.Empty, prazo.Token);
            }

            await smtp.SendAsync(mensagem, prazo.Token);
            await smtp.DisconnectAsync(true, prazo.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"O envio do e-mail da OS {numeroOs} passou de {configuracao.TimeoutEmSegundos}s.");
        }
    }
}
