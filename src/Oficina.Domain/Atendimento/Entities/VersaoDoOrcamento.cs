using System.Security.Cryptography;

namespace Oficina.Domain.Atendimento.Entities;

public enum SituacaoDaVersao
{
    Pendente = 1,
    Aprovada = 2,
    Recusada = 3,
    Superada = 4
}

public enum TipoDoItemDoOrcamento
{
    Servico = 1,
    PecaInsumo = 2
}

public sealed class ItemDaVersao
{
    private ItemDaVersao()
    {
        Descricao = null!;
    }

    internal ItemDaVersao(TipoDoItemDoOrcamento tipo, Guid referenciaId, string descricao, decimal quantidade, decimal precoUnitario)
    {
        Tipo = tipo;
        ReferenciaId = referenciaId;
        Descricao = descricao;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }

    public int Id { get; private set; }

    public TipoDoItemDoOrcamento Tipo { get; private set; }

    public Guid ReferenciaId { get; private set; }

    public string Descricao { get; private set; }

    public decimal Quantidade { get; private set; }

    public decimal PrecoUnitario { get; private set; }

    public decimal Subtotal => Quantidade * PrecoUnitario;
}

public sealed class VersaoDoOrcamento
{
    private readonly List<ItemDaVersao> _itens = [];

    private VersaoDoOrcamento()
    {
        Token = null!;
    }

    internal VersaoDoOrcamento(int numero, IEnumerable<ItemDaVersao> itens, DateTimeOffset geradaEm)
    {
        Numero = numero;
        GeradaEm = geradaEm;
        Situacao = SituacaoDaVersao.Pendente;
        Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _itens.AddRange(itens);
        Total = _itens.Sum(i => i.Subtotal);
    }

    public int Numero { get; private set; }

    public DateTimeOffset GeradaEm { get; private set; }

    public decimal Total { get; private set; }

    public SituacaoDaVersao Situacao { get; private set; }

    public string Token { get; private set; }

    public string? MotivoDaRecusa { get; private set; }

    public IReadOnlyList<ItemDaVersao> Itens => _itens;

    internal void Superar()
    {
        if (Situacao == SituacaoDaVersao.Pendente)
        {
            Situacao = SituacaoDaVersao.Superada;
        }
    }
}
