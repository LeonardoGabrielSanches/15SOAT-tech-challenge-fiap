using Oficina.Domain.Common;

namespace Oficina.Domain.Estoque;

public sealed class PecaInsumo : RaizDeAgregado
{
    private PecaInsumo()
    {
        Nome = string.Empty;
        UnidadeDeMedida = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public TipoPecaInsumo Tipo { get; private set; }

    public string UnidadeDeMedida { get; private set; }

    public Dinheiro Preco { get; private set; }

    public decimal Saldo { get; private set; }

    public bool Ativo { get; private set; }

    public static PecaInsumo Criar(string nome, TipoPecaInsumo tipo, string unidadeDeMedida, Dinheiro preco)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new DadosInvalidosException("Tipo de peça/insumo inválido.");
        }

        var item = new PecaInsumo { Id = Guid.NewGuid(), Tipo = tipo, Ativo = true };
        item.Atualizar(nome, unidadeDeMedida, preco);
        return item;
    }

    public void Atualizar(string nome, string unidadeDeMedida, Dinheiro preco)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DadosInvalidosException("O nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(unidadeDeMedida))
        {
            throw new DadosInvalidosException("A unidade de medida é obrigatória.");
        }

        Nome = nome.Trim();
        UnidadeDeMedida = unidadeDeMedida.Trim();
        Preco = preco;
    }

    public void RegistrarEntrada(Quantidade quantidade)
    {
        GarantirAtivo();
        GarantirTipo(quantidade);
        Saldo += quantidade.Valor;
    }

    public void Baixar(Quantidade quantidade)
    {
        GarantirAtivo();
        GarantirTipo(quantidade);
        if (quantidade.Valor > Saldo)
        {
            throw new DadosInvalidosException($"Saldo insuficiente de {Nome}: disponível {Saldo}, solicitado {quantidade.Valor}.");
        }

        Saldo -= quantidade.Valor;
    }

    public void Estornar(Quantidade quantidade)
    {
        GarantirTipo(quantidade);
        Saldo += quantidade.Valor;
    }

    public void Inativar() => Ativo = false;

    private void GarantirAtivo()
    {
        if (!Ativo)
        {
            throw new ConflitoDeEstadoException($"{Nome} está inativo.");
        }
    }

    private void GarantirTipo(Quantidade quantidade)
    {
        if (quantidade.Tipo != Tipo)
        {
            throw new DadosInvalidosException($"A quantidade informada não é de {Tipo}.");
        }
    }
}
