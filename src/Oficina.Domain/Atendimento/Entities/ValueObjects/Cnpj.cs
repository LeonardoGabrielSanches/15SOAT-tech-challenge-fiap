using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities.ValueObjects;

public sealed record Cnpj
{
    private static readonly int[] PesosDoPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosDoSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private Cnpj(string numero) => Numero = numero;

    public string Numero { get; }

    public static Cnpj Criar(string? valor)
    {
        var numero = new string((valor ?? "")
            .Where(c => !char.IsWhiteSpace(c) && c is not '.' and not '/' and not '-')
            .Select(char.ToUpperInvariant)
            .ToArray());

        if (numero.Length != 14
            || !numero.Take(12).All(EhCaractereValido)
            || !numero.Skip(12).All(char.IsAsciiDigit)
            || numero.Distinct().Count() == 1)
        {
            throw new DadosInvalidosException("CNPJ invalido.");
        }

        var primeiro = CalcularDigito(numero.AsSpan(0, 12), PesosDoPrimeiroDigito);
        var segundo = CalcularDigito(numero.AsSpan(0, 13), PesosDoSegundoDigito);
        if (numero[12] - '0' != primeiro || numero[13] - '0' != segundo)
        {
            throw new DadosInvalidosException("CNPJ invalido.");
        }

        return new Cnpj(numero);
    }

    private static bool EhCaractereValido(char c) => char.IsAsciiDigit(c) || c is >= 'A' and <= 'Z';

    private static int CalcularDigito(ReadOnlySpan<char> baseDoCalculo, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < baseDoCalculo.Length; i++)
        {
            soma += (baseDoCalculo[i] - '0') * pesos[i];
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Numero;
}
