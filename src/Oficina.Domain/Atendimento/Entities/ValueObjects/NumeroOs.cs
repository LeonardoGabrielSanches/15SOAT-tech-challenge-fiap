using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities.ValueObjects;

public sealed record NumeroOs
{
    private const int AnoMinimo = 1000;
    private const int AnoMaximo = 9999;
    private const int SequenciaMaxima = 99999;

    private NumeroOs(string valor) => Valor = valor;

    public string Valor { get; }

    public static NumeroOs Criar(int ano, int sequencia)
    {
        if (ano is < AnoMinimo or > AnoMaximo || sequencia is < 1 or > SequenciaMaxima)
        {
            throw new DadosInvalidosException("Numero da OS invalido.");
        }

        return new NumeroOs($"OS-{ano:D4}-{sequencia:D5}");
    }

    public static NumeroOs Reconstituir(string valor) => new(valor);

    public override string ToString() => Valor;
}
