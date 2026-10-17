using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities.ValueObjects;

public sealed record Email
{
    private Email(string endereco) => Endereco = endereco;

    public string Endereco { get; }

    public static Email Criar(string? valor)
    {
        var endereco = (valor ?? "").Trim().ToLowerInvariant();
        var partes = endereco.Split('@');

        var valido = endereco.Length <= 254
            && partes.Length == 2
            && partes[0].Length > 0
            && !endereco.Any(char.IsWhiteSpace)
            && partes[1].Contains('.')
            && !partes[1].StartsWith('.')
            && !partes[1].EndsWith('.');

        if (!valido)
        {
            throw new DadosInvalidosException("E-mail invalido.");
        }

        return new Email(endereco);
    }

    public override string ToString() => Endereco;
}
