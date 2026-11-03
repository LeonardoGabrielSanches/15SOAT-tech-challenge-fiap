using MailKit.Security;

namespace Oficina.Infrastructure.Notificacao;

public sealed class OpcoesDoEmail
{
    public const string Secao = "Email";

    public string Host { get; init; } = "localhost";
    public int Porta { get; init; } = 1025;
    public string? Usuario { get; init; }
    public string? Senha { get; init; }
    public string Remetente { get; init; } = "Oficina <nao-responda@oficina.local>";
    public SecureSocketOptions Tls { get; init; } = SecureSocketOptions.None;
    public int TimeoutEmSegundos { get; init; } = 5;
    public string UrlDoPortal { get; init; } = "http://localhost:3000";
}
