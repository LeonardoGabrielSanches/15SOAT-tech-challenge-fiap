namespace Oficina.Api.Autenticacao;

public static class Politicas
{
    public const string Atendente = "atendente";
    public const string Mecanico = "mecanico";
    public const string Gerente = "gerente";
    public const string AtendenteOuMecanico = "atendente-ou-mecanico";

    public static readonly IReadOnlySet<string> Todas = new HashSet<string> { Atendente, Mecanico, Gerente };
}
