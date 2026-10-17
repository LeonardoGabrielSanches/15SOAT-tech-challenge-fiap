namespace Oficina.Api.Autenticacao;

public sealed class OpcoesDoKeycloak
{
    public const string Secao = "Keycloak";

    public string Authority { get; set; } = "";

    public string UrlPublica { get; set; } = "";

    public string Audience { get; set; } = "oficina-api";

    public string ClienteSwagger { get; set; } = "oficina-swagger";

    public bool ExigirHttps { get; set; }
}
