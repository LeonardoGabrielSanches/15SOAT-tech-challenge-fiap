using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities.ValueObjects;

public sealed record Cpf
{
    private Cpf(string numero) => Numero = numero;

    public string Numero { get; }

    public static Cpf Criar(string? valor)
    {
        var numero = new string((valor ?? "").Where(c => !char.IsWhiteSpace(c) && c is not '.' and not '-').ToArray());

        if (numero.Length != 11 || !numero.All(char.IsAsciiDigit) || numero.Distinct().Count() == 1)
        {
            throw new DadosInvalidosException("CPF invalido.");
        }

        var primeiro = CalcularDigito(numero.AsSpan(0, 9), 10);
        var segundo = CalcularDigito(numero.AsSpan(0, 10), 11);
        if (numero[9] - '0' != primeiro || numero[10] - '0' != segundo)
        {
            throw new DadosInvalidosException("CPF invalido.");
        }

        return new Cpf(numero);
    }

    private static int CalcularDigito(ReadOnlySpan<char> baseDoCalculo, int pesoInicial)
    {
        var soma = 0;
        for (var i = 0; i < baseDoCalculo.Length; i++)
        {
            soma += (baseDoCalculo[i] - '0') * (pesoInicial - i);
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Numero;
}
