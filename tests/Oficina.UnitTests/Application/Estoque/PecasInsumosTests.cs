using Moq;
using Oficina.Application.Common;
using Oficina.Application.Estoque;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.UnitTests.Application.Estoque;

public class PecasInsumosTests
{
    private readonly Mock<IPecaInsumoRepository> _repositorio = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidadeDeTrabalho = new();

    private PecaInsumo ItemExistente(TipoPecaInsumo tipo = TipoPecaInsumo.Peca, decimal saldo = 0)
    {
        var item = PecaInsumo.Criar("Filtro", tipo, "un", Dinheiro.De(10m));
        if (saldo > 0)
        {
            item.RegistrarEntrada(Quantidade.Criar(saldo, tipo));
        }

        _repositorio.Setup(r => r.Obter(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        return item;
    }

    [Fact]
    public async Task Cadastrar_adiciona_o_item_e_confirma()
    {
        var caso = new CadastrarPecaInsumo(_repositorio.Object, _unidadeDeTrabalho.Object);

        var dto = await caso.Executar(new CadastrarPecaInsumoComando("Óleo", TipoPecaInsumo.Insumo, "L", 42.9m), default);

        Assert.Equal(0m, dto.Saldo);
        Assert.True(dto.Ativo);
        _repositorio.Verify(r => r.Adicionar(It.Is<PecaInsumo>(i => i.Id == dto.Id && i.Nome == "Óleo")), Times.Once);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cadastrar_com_dados_invalidos_nao_confirma()
    {
        var caso = new CadastrarPecaInsumo(_repositorio.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<DadosInvalidosException>(() =>
            caso.Executar(new CadastrarPecaInsumoComando("", TipoPecaInsumo.Peca, "un", 1m), default));

        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Atualizar_altera_os_dados_e_confirma()
    {
        var item = ItemExistente();
        var caso = new AtualizarPecaInsumo(_repositorio.Object, _unidadeDeTrabalho.Object);

        var dto = await caso.Executar(item.Id, new AtualizarPecaInsumoComando("Filtro novo", "cx", 12.5m), default);

        Assert.Equal("Filtro novo", dto.Nome);
        Assert.Equal(12.5m, dto.Preco);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Atualizar_item_inexistente_falha_com_nao_encontrado()
    {
        var caso = new AtualizarPecaInsumo(_repositorio.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            caso.Executar(Guid.NewGuid(), new AtualizarPecaInsumoComando("x", "un", 1m), default));
    }

    [Fact]
    public async Task Inativar_marca_o_item_como_inativo_e_confirma()
    {
        var item = ItemExistente();
        var caso = new InativarPecaInsumo(_repositorio.Object, _unidadeDeTrabalho.Object);

        await caso.Executar(item.Id, default);

        Assert.False(item.Ativo);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Entrada_aumenta_o_saldo_e_confirma()
    {
        var item = ItemExistente(TipoPecaInsumo.Insumo, saldo: 2m);
        var caso = new RegistrarEntradaDeMercadoria(_repositorio.Object, _unidadeDeTrabalho.Object);

        var saldo = await caso.Executar(item.Id, 3.5m, default);

        Assert.Equal(5.5m, saldo.Saldo);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Entrada_fracionada_de_peca_e_rejeitada_sem_confirmar()
    {
        var item = ItemExistente(TipoPecaInsumo.Peca);
        var caso = new RegistrarEntradaDeMercadoria(_repositorio.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<DadosInvalidosException>(() => caso.Executar(item.Id, 1.5m, default));

        Assert.Equal(0m, item.Saldo);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Entrada_em_item_inexistente_falha_com_nao_encontrado()
    {
        var caso = new RegistrarEntradaDeMercadoria(_repositorio.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(Guid.NewGuid(), 1m, default));
    }

    [Fact]
    public async Task Consultar_saldo_retorna_o_saldo_atual()
    {
        var item = ItemExistente(saldo: 7m);
        var caso = new ConsultarSaldo(_repositorio.Object);

        var saldo = await caso.Executar(item.Id, default);

        Assert.Equal(7m, saldo.Saldo);
    }

    [Fact]
    public async Task Consultar_retorna_o_item_e_inexistente_falha_com_nao_encontrado()
    {
        var item = ItemExistente(saldo: 3m);
        var caso = new ConsultarPecaInsumo(_repositorio.Object);

        var dto = await caso.Executar(item.Id, default);

        Assert.Equal((item.Id, 3m), (dto.Id, dto.Saldo));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Listar_repassa_o_filtro_de_inativos()
    {
        var item = ItemExistente();
        _repositorio.Setup(r => r.Listar(true, It.IsAny<CancellationToken>())).ReturnsAsync([item]);
        var caso = new ListarPecasInsumos(_repositorio.Object);

        var itens = await caso.Executar(true, default);

        Assert.Single(itens);
    }
}
