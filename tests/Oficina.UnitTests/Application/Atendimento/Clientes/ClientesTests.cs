using Moq;
using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.Application.Atendimento.Clientes;

public class ClientesTests
{
    private const string CpfValido = "529.982.247-25";

    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidadeDeTrabalho = new();

    private static Cliente NovoCliente(string documento = CpfValido) =>
        Cliente.Cadastrar("Maria Silva", Documento.Criar(documento), Email.Criar("maria@oficina.com"), "11999990000");

    [Fact]
    public async Task Cadastrar_persiste_o_cliente_e_confirma_a_unidade_de_trabalho()
    {
        var caso = new CadastrarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        var dto = await caso.Executar(
            new CadastrarClienteComando("  Maria Silva ", CpfValido, "Maria@Oficina.com", " 11999990000 "),
            CancellationToken.None);

        Assert.Equal("Maria Silva", dto.Nome);
        Assert.Equal("Cpf", dto.TipoDocumento);
        Assert.Equal("52998224725", dto.Documento);
        Assert.Equal("maria@oficina.com", dto.Email);
        Assert.Equal("11999990000", dto.Telefone);
        Assert.True(dto.Ativo);
        _clientes.Verify(r => r.Adicionar(It.Is<Cliente>(c => c.Id == dto.Id && c.Ativo)), Times.Once);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cadastrar_aceita_cnpj_alfanumerico_e_telefone_ausente()
    {
        var caso = new CadastrarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        var dto = await caso.Executar(
            new CadastrarClienteComando("Auto Pecas", "12.ABC.345/01DE-35", "contato@autopecas.com", null),
            CancellationToken.None);

        Assert.Equal(TipoDocumento.Cnpj.ToString(), dto.TipoDocumento);
        Assert.Equal("12ABC34501DE35", dto.Documento);
        Assert.Null(dto.Telefone);
    }

    [Fact]
    public async Task Cadastrar_com_documento_ja_cadastrado_e_rejeitado_como_conflito()
    {
        _clientes
            .Setup(r => r.ObterPorDocumento(Documento.Criar(CpfValido), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NovoCliente());
        var caso = new CadastrarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => caso.Executar(
            new CadastrarClienteComando("Outra Maria", "52998224725", "outra@oficina.com", null),
            CancellationToken.None));

        _clientes.Verify(r => r.Adicionar(It.IsAny<Cliente>()), Times.Never);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Maria", "52998224724", "maria@oficina.com")]
    [InlineData("Maria", CpfValido, "sem-arroba")]
    [InlineData("Maria", CpfValido, "")]
    [InlineData("", CpfValido, "maria@oficina.com")]
    [InlineData("   ", CpfValido, "maria@oficina.com")]
    public async Task Cadastrar_com_dados_invalidos_nao_persiste(string nome, string documento, string email)
    {
        var caso = new CadastrarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<DadosInvalidosException>(() =>
            caso.Executar(new CadastrarClienteComando(nome, documento, email, null), CancellationToken.None));

        _clientes.Verify(r => r.Adicionar(It.IsAny<Cliente>()), Times.Never);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cadastrar_rejeita_nome_e_telefone_acima_do_limite()
    {
        var caso = new CadastrarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<DadosInvalidosException>(() => caso.Executar(
            new CadastrarClienteComando(new string('a', 201), CpfValido, "maria@oficina.com", null),
            CancellationToken.None));
        await Assert.ThrowsAsync<DadosInvalidosException>(() => caso.Executar(
            new CadastrarClienteComando("Maria", CpfValido, "maria@oficina.com", new string('9', 21)),
            CancellationToken.None));
    }

    [Fact]
    public async Task Consultar_retorna_o_cliente_inclusive_inativo()
    {
        var cliente = NovoCliente();
        cliente.Inativar();
        _clientes.Setup(r => r.ObterPorId(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var caso = new ConsultarCliente(_clientes.Object);

        var dto = await caso.Executar(cliente.Id, CancellationToken.None);

        Assert.Equal(cliente.Id, dto.Id);
        Assert.False(dto.Ativo);
    }

    [Fact]
    public async Task Consultar_cliente_inexistente_falha_com_recurso_nao_encontrado()
    {
        var caso = new ConsultarCliente(_clientes.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Listar_repassa_o_filtro_de_situacao_e_de_documento_normalizado()
    {
        var cliente = NovoCliente();
        _clientes
            .Setup(r => r.Listar(true, Documento.Criar(CpfValido), It.IsAny<CancellationToken>()))
            .ReturnsAsync([cliente]);
        var caso = new ListarClientes(_clientes.Object);

        var lista = await caso.Executar(new ListarClientesFiltro(true, CpfValido), CancellationToken.None);

        Assert.Equal(cliente.Id, Assert.Single(lista).Id);
    }

    [Fact]
    public async Task Listar_sem_filtros_pede_todos_ao_repositorio()
    {
        _clientes
            .Setup(r => r.Listar(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([NovoCliente(), NovoCliente("11.222.333/0001-81")]);
        var caso = new ListarClientes(_clientes.Object);

        var lista = await caso.Executar(new ListarClientesFiltro(null, null), CancellationToken.None);

        Assert.Equal(2, lista.Count);
    }

    [Fact]
    public async Task Listar_com_documento_invalido_falha()
    {
        var caso = new ListarClientes(_clientes.Object);

        await Assert.ThrowsAsync<DadosInvalidosException>(() =>
            caso.Executar(new ListarClientesFiltro(null, "123"), CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_altera_os_dados_de_contato_e_preserva_o_documento()
    {
        var cliente = NovoCliente();
        _clientes.Setup(r => r.ObterPorId(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var caso = new AtualizarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        var dto = await caso.Executar(
            new AtualizarClienteComando(cliente.Id, "Maria Souza", "novo@oficina.com", null),
            CancellationToken.None);

        Assert.Equal("Maria Souza", dto.Nome);
        Assert.Equal("novo@oficina.com", dto.Email);
        Assert.Null(dto.Telefone);
        Assert.Equal("52998224725", dto.Documento);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Atualizar_cliente_inexistente_falha_com_recurso_nao_encontrado()
    {
        var caso = new AtualizarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(
            new AtualizarClienteComando(Guid.NewGuid(), "Maria", "maria@oficina.com", null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_com_email_invalido_nao_confirma()
    {
        var cliente = NovoCliente();
        _clientes.Setup(r => r.ObterPorId(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var caso = new AtualizarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<DadosInvalidosException>(() => caso.Executar(
            new AtualizarClienteComando(cliente.Id, "Maria", "invalido", null),
            CancellationToken.None));

        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Atualizar_cliente_inativo_e_rejeitado_como_conflito()
    {
        var cliente = NovoCliente();
        cliente.Inativar();
        _clientes.Setup(r => r.ObterPorId(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var caso = new AtualizarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => caso.Executar(
            new AtualizarClienteComando(cliente.Id, "Maria", "maria@oficina.com", null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Inativar_marca_o_cliente_como_inativo_sem_excluir()
    {
        var cliente = NovoCliente();
        _clientes.Setup(r => r.ObterPorId(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var caso = new InativarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        await caso.Executar(cliente.Id, CancellationToken.None);

        Assert.False(cliente.Ativo);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inativar_cliente_inexistente_falha_com_recurso_nao_encontrado()
    {
        var caso = new InativarCliente(_clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public void Cliente_inativo_nao_pode_ser_usado_em_novas_operacoes()
    {
        var cliente = NovoCliente();
        cliente.GarantirAtivo();
        cliente.Inativar();

        Assert.Throws<ConflitoDeEstadoException>(cliente.GarantirAtivo);
    }
}
