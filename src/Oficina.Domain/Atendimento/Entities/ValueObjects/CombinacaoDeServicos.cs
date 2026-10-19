namespace Oficina.Domain.Atendimento.Entities.ValueObjects;

public sealed record CombinacaoDeServicos
{
    private CombinacaoDeServicos(IReadOnlyList<Guid> servicoIds) => ServicoIds = servicoIds;

    public IReadOnlyList<Guid> ServicoIds { get; }

    public static CombinacaoDeServicos De(IEnumerable<Guid> servicoIds) =>
        new(servicoIds.Distinct().Order().ToList());

    public bool Equals(CombinacaoDeServicos? outra) =>
        outra is not null && ServicoIds.SequenceEqual(outra.ServicoIds);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var id in ServicoIds)
        {
            hash.Add(id);
        }

        return hash.ToHashCode();
    }
}
