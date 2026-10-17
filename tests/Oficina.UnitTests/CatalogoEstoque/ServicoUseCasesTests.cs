using Moq;
using Oficina.Application.CatalogoEstoque.UseCases;
using Oficina.Application.Common;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.CatalogoEstoque;

public class ServicoUseCasesTests
{
    private readonly Mock<IServicoRepository> _repositorio = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidade = new();

    private static Servico NovoServico(string nome = "Troca de oleo", decimal preco = 120m) =>
        Servico.Cadastrar(nome, "Descricao", Dinheiro.De(preco));

    [Fact]
    public async Task Cadastrar_persiste_o_servico_ativo_e_confirma()
    {
        Servico? adicionado = null;
        _repositorio.Setup(r => r.Adicionar(It.IsAny<Servico>())).Callback<Servico>(s => adicionado = s);

        var dto = await new CadastrarServico(_repositorio.Object, _unidade.Object)
            .Executar(new DadosDoServico("Alinhamento", "Dianteiro", 80m), CancellationToken.None);

        Assert.NotNull(adicionado);
        Assert.Equal("Alinhamento", dto.Nome);
        Assert.Equal(80m, dto.Preco);
        Assert.True(dto.Ativo);
        Assert.Equal(adicionado.Id, dto.Id);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("   ", 10)]
    [InlineData("Nome", -1)]
    public async Task Cadastrar_rejeita_dados_invalidos_sem_persistir(string nome, decimal preco)
    {
        var caso = new CadastrarServico(_repositorio.Object, _unidade.Object);

        await Assert.ThrowsAsync<DadosInvalidosException>(() =>
            caso.Executar(new DadosDoServico(nome, null, preco), CancellationToken.None));

        _repositorio.Verify(r => r.Adicionar(It.IsAny<Servico>()), Times.Never);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consultar_devolve_o_servico()
    {
        var servico = NovoServico();
        _repositorio.Setup(r => r.Obter(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);

        var dto = await new ConsultarServico(_repositorio.Object).Executar(servico.Id, CancellationToken.None);

        Assert.Equal(servico.Id, dto.Id);
        Assert.Equal(120m, dto.Preco);
    }

    [Fact]
    public async Task Consultar_inexistente_lanca_recurso_nao_encontrado()
    {
        _repositorio.Setup(r => r.Obter(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Servico?)null);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            new ConsultarServico(_repositorio.Object).Executar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Listar_inclui_inativos()
    {
        var inativo = NovoServico("Antigo");
        inativo.Inativar();
        _repositorio.Setup(r => r.Listar(It.IsAny<CancellationToken>())).ReturnsAsync([NovoServico(), inativo]);

        var lista = await new ListarServicos(_repositorio.Object).Executar(CancellationToken.None);

        Assert.Equal(2, lista.Count);
        Assert.Contains(lista, s => !s.Ativo);
    }

    [Fact]
    public async Task Atualizar_altera_os_dados_e_confirma()
    {
        var servico = NovoServico();
        _repositorio.Setup(r => r.Obter(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);

        var dto = await new AtualizarServico(_repositorio.Object, _unidade.Object)
            .Executar(servico.Id, new DadosDoServico("Novo nome", "Nova", 200m), CancellationToken.None);

        Assert.Equal("Novo nome", dto.Nome);
        Assert.Equal("Nova", dto.Descricao);
        Assert.Equal(200m, dto.Preco);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Atualizar_inexistente_lanca_recurso_nao_encontrado()
    {
        _repositorio.Setup(r => r.Obter(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Servico?)null);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            new AtualizarServico(_repositorio.Object, _unidade.Object)
                .Executar(Guid.NewGuid(), new DadosDoServico("X", null, 1m), CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_com_preco_negativo_nao_confirma()
    {
        var servico = NovoServico();
        _repositorio.Setup(r => r.Obter(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);

        await Assert.ThrowsAsync<DadosInvalidosException>(() =>
            new AtualizarServico(_repositorio.Object, _unidade.Object)
                .Executar(servico.Id, new DadosDoServico("X", null, -5m), CancellationToken.None));

        Assert.Equal(120m, servico.Preco.Valor);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Inativar_marca_o_servico_como_inativo_e_confirma()
    {
        var servico = NovoServico();
        _repositorio.Setup(r => r.Obter(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);

        await new InativarServico(_repositorio.Object, _unidade.Object).Executar(servico.Id, CancellationToken.None);

        Assert.False(servico.Ativo);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inativar_inexistente_lanca_recurso_nao_encontrado()
    {
        _repositorio.Setup(r => r.Obter(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Servico?)null);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            new InativarServico(_repositorio.Object, _unidade.Object).Executar(Guid.NewGuid(), CancellationToken.None));
    }
}
