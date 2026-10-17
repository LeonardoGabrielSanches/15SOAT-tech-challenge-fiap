using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;

namespace Oficina.UnitTests.Domain;

public class PecaInsumoTests
{
    private static PecaInsumo NovoOleo() =>
        PecaInsumo.Criar("Óleo 5W30", TipoPecaInsumo.Insumo, "L", Dinheiro.De(42.90m));

    private static PecaInsumo NovoFiltro() =>
        PecaInsumo.Criar("Filtro de óleo", TipoPecaInsumo.Peca, "un", Dinheiro.De(25m));

    [Fact]
    public void Item_novo_nasce_ativo_com_saldo_zero()
    {
        var item = NovoOleo();

        Assert.True(item.Ativo);
        Assert.Equal(0m, item.Saldo);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Theory]
    [InlineData("", "L")]
    [InlineData("  ", "L")]
    [InlineData("Óleo", "")]
    public void Rejeita_nome_ou_unidade_vazios(string nome, string unidade)
    {
        Assert.Throws<DadosInvalidosException>(() =>
            PecaInsumo.Criar(nome, TipoPecaInsumo.Insumo, unidade, Dinheiro.De(1m)));
    }

    [Fact]
    public void Entrada_aumenta_o_saldo()
    {
        var item = NovoOleo();

        item.RegistrarEntrada(Quantidade.Criar(10m, TipoPecaInsumo.Insumo));
        item.RegistrarEntrada(Quantidade.Criar(2.5m, TipoPecaInsumo.Insumo));

        Assert.Equal(12.5m, item.Saldo);
    }

    [Fact]
    public void Quantidade_de_outro_tipo_e_rejeitada()
    {
        var item = NovoFiltro();

        Assert.Throws<DadosInvalidosException>(() =>
            item.RegistrarEntrada(Quantidade.Criar(1.5m, TipoPecaInsumo.Insumo)));
    }

    [Fact]
    public void Baixa_reduz_o_saldo()
    {
        var item = NovoFiltro();
        item.RegistrarEntrada(Quantidade.Criar(5m, TipoPecaInsumo.Peca));

        item.Baixar(Quantidade.Criar(3m, TipoPecaInsumo.Peca));

        Assert.Equal(2m, item.Saldo);
    }

    [Fact]
    public void Baixa_pode_zerar_o_saldo()
    {
        var item = NovoFiltro();
        item.RegistrarEntrada(Quantidade.Criar(5m, TipoPecaInsumo.Peca));

        item.Baixar(Quantidade.Criar(5m, TipoPecaInsumo.Peca));

        Assert.Equal(0m, item.Saldo);
    }

    [Fact]
    public void Baixa_maior_que_o_saldo_e_rejeitada_e_preserva_o_saldo()
    {
        var item = NovoFiltro();
        item.RegistrarEntrada(Quantidade.Criar(2m, TipoPecaInsumo.Peca));

        Assert.Throws<DadosInvalidosException>(() => item.Baixar(Quantidade.Criar(3m, TipoPecaInsumo.Peca)));

        Assert.Equal(2m, item.Saldo);
    }

    [Fact]
    public void Estorno_devolve_ao_saldo()
    {
        var item = NovoOleo();
        item.RegistrarEntrada(Quantidade.Criar(10m, TipoPecaInsumo.Insumo));
        item.Baixar(Quantidade.Criar(4.5m, TipoPecaInsumo.Insumo));

        item.Estornar(Quantidade.Criar(1.5m, TipoPecaInsumo.Insumo));

        Assert.Equal(7m, item.Saldo);
    }

    [Fact]
    public void Item_inativo_nao_aceita_entrada_nem_baixa()
    {
        var item = NovoFiltro();
        item.RegistrarEntrada(Quantidade.Criar(5m, TipoPecaInsumo.Peca));
        item.Inativar();

        Assert.False(item.Ativo);
        Assert.Throws<ConflitoDeEstadoException>(() => item.RegistrarEntrada(Quantidade.Criar(1m, TipoPecaInsumo.Peca)));
        Assert.Throws<ConflitoDeEstadoException>(() => item.Baixar(Quantidade.Criar(1m, TipoPecaInsumo.Peca)));
    }

    [Fact]
    public void Item_inativo_aceita_estorno()
    {
        var item = NovoFiltro();
        item.RegistrarEntrada(Quantidade.Criar(5m, TipoPecaInsumo.Peca));
        item.Baixar(Quantidade.Criar(2m, TipoPecaInsumo.Peca));
        item.Inativar();

        item.Estornar(Quantidade.Criar(2m, TipoPecaInsumo.Peca));

        Assert.Equal(5m, item.Saldo);
    }

    [Fact]
    public void Atualiza_nome_unidade_e_preco()
    {
        var item = NovoOleo();

        item.Atualizar("Óleo 10W40", "ml", Dinheiro.De(0.05m));

        Assert.Equal("Óleo 10W40", item.Nome);
        Assert.Equal("ml", item.UnidadeDeMedida);
        Assert.Equal(Dinheiro.De(0.05m), item.Preco);
    }

}
