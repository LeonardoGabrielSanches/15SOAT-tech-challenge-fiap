using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities.ValueObjects;

public sealed record AnoFabricacao
{
    private const int AnoMinimo = 1900;

    private AnoFabricacao(int valor) => Valor = valor;

    public int Valor { get; }

    public static AnoFabricacao Criar(int ano, TimeProvider relogio)
    {
        var anoMaximo = relogio.GetUtcNow().Year + 1;
        if (ano < AnoMinimo || ano > anoMaximo)
        {
            throw new DadosInvalidosException($"Ano de fabricacao deve estar entre {AnoMinimo} e {anoMaximo}.");
        }

        return new AnoFabricacao(ano);
    }
}
