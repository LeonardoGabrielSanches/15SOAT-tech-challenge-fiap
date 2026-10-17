using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities.ValueObjects;

public sealed record Placa
{
    private Placa(string valor) => Valor = valor;

    public string Valor { get; }

    public static Placa Criar(string? entrada)
    {
        var bruto = (entrada ?? "").Trim().ToUpperInvariant();
        var valor = bruto.Length == 8 && bruto[3] == '-' ? bruto.Remove(3, 1) : bruto;

        var valida = valor.Length == 7
            && valor[..3].All(char.IsAsciiLetterUpper)
            && char.IsAsciiDigit(valor[3])
            && (char.IsAsciiDigit(valor[4]) || char.IsAsciiLetterUpper(valor[4]))
            && valor[5..].All(char.IsAsciiDigit);

        if (!valida)
        {
            throw new DadosInvalidosException("Placa invalida.");
        }

        return new Placa(valor);
    }

    public override string ToString() => Valor;
}
