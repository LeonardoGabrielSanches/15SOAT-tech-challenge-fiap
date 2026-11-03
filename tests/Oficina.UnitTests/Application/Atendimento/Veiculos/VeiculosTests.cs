using Moq;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;
using Oficina.UnitTests.Domain.Atendimento;

namespace Oficina.UnitTests.Application.Atendimento.Veiculos;

public class VeiculosTests
{
    private readonly Mock<IVeiculoRepository> _veiculos = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidadeDeTrabalho = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero));

    private static Cliente NovoCliente(string cpf = "529.982.247-25") =>
        Cliente.Cadastrar("Maria Silva", Documento.Criar(cpf), Email.Criar("maria@oficina.com"), null);

    private Veiculo NovoVeiculo(Cliente proprietario, string placa = "ABC1D23") =>
        Veiculo.Cadastrar(Placa.Criar(placa), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), proprietario);

    private Cliente ClienteExistente()
    {
        var cliente = NovoCliente();
        _clientes.Setup(r => r.ObterPorId(cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        return cliente;
    }

    private CadastrarVeiculo Cadastro() =>
        new(_veiculos.Object, _clientes.Object, _unidadeDeTrabalho.Object, _relogio);

    [Fact]
    public async Task Cadastrar_normaliza_placa_vincula_proprietario_e_confirma()
    {
        var cliente = ClienteExistente();

        var dto = await Cadastro().Executar(
            new CadastrarVeiculoComando("abc-1d23", " Fiat ", " Uno ", 2015, cliente.Id), CancellationToken.None);

        Assert.Equal("ABC1D23", dto.Placa);
        Assert.Equal("Fiat", dto.Marca);
        Assert.Equal("Uno", dto.Modelo);
        Assert.Equal(cliente.Id, dto.ProprietarioId);
        Assert.True(dto.Ativo);
        _veiculos.Verify(r => r.Adicionar(It.Is<Veiculo>(v => v.Id == dto.Id)), Times.Once);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cadastrar_com_placa_duplicada_e_rejeitado_como_conflito()
    {
        var cliente = ClienteExistente();
        _veiculos.Setup(r => r.ObterPorPlaca(Placa.Criar("ABC1234"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NovoVeiculo(cliente, "ABC1234"));

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Cadastro().Executar(
            new CadastrarVeiculoComando("ABC-1234", "Fiat", "Uno", 2015, cliente.Id), CancellationToken.None));

        _veiculos.Verify(r => r.Adicionar(It.IsAny<Veiculo>()), Times.Never);
    }

    [Fact]
    public async Task Cadastrar_com_cliente_inexistente_e_rejeitado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => Cadastro().Executar(
            new CadastrarVeiculoComando("ABC1234", "Fiat", "Uno", 2015, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Cadastrar_com_cliente_inativo_e_rejeitado()
    {
        var cliente = ClienteExistente();
        cliente.Inativar();

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Cadastro().Executar(
            new CadastrarVeiculoComando("ABC1234", "Fiat", "Uno", 2015, cliente.Id), CancellationToken.None));

        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cadastrar_com_ano_fora_do_intervalo_e_rejeitado()
    {
        var cliente = ClienteExistente();

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Cadastro().Executar(
            new CadastrarVeiculoComando("ABC1234", "Fiat", "Uno", 2028, cliente.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Transferir_troca_o_proprietario_e_confirma()
    {
        var antigo = NovoCliente();
        var novo = NovoCliente("111.444.777-35");
        var veiculo = NovoVeiculo(antigo);
        _veiculos.Setup(r => r.ObterPorId(veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        _clientes.Setup(r => r.ObterPorId(novo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(novo);
        var caso = new TransferirVeiculo(_veiculos.Object, _clientes.Object, _unidadeDeTrabalho.Object);

        var dto = await caso.Executar(new TransferirVeiculoComando(veiculo.Id, novo.Id), CancellationToken.None);

        Assert.Equal(novo.Id, dto.ProprietarioId);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Transferir_para_cliente_inexistente_e_rejeitado()
    {
        var veiculo = NovoVeiculo(NovoCliente());
        _veiculos.Setup(r => r.ObterPorId(veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        var caso = new TransferirVeiculo(_veiculos.Object, _clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            caso.Executar(new TransferirVeiculoComando(veiculo.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Transferir_para_cliente_inativo_e_rejeitado()
    {
        var antigo = NovoCliente();
        var novo = NovoCliente("111.444.777-35");
        novo.Inativar();
        var veiculo = NovoVeiculo(antigo);
        _veiculos.Setup(r => r.ObterPorId(veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        _clientes.Setup(r => r.ObterPorId(novo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(novo);
        var caso = new TransferirVeiculo(_veiculos.Object, _clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            caso.Executar(new TransferirVeiculoComando(veiculo.Id, novo.Id), CancellationToken.None));

        Assert.Equal(antigo.Id, veiculo.ProprietarioId);
    }

    [Fact]
    public async Task Transferir_veiculo_inexistente_e_nao_encontrado()
    {
        var caso = new TransferirVeiculo(_veiculos.Object, _clientes.Object, _unidadeDeTrabalho.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            caso.Executar(new TransferirVeiculoComando(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Atualizar_altera_marca_modelo_e_ano()
    {
        var veiculo = NovoVeiculo(NovoCliente());
        _veiculos.Setup(r => r.ObterPorId(veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);
        var caso = new AtualizarVeiculo(_veiculos.Object, _unidadeDeTrabalho.Object, _relogio);

        var dto = await caso.Executar(new AtualizarVeiculoComando(veiculo.Id, "VW", "Gol", 2020), CancellationToken.None);

        Assert.Equal(("VW", "Gol", 2020), (dto.Marca, dto.Modelo, dto.AnoFabricacao));
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inativar_marca_o_veiculo_como_inativo()
    {
        var veiculo = NovoVeiculo(NovoCliente());
        _veiculos.Setup(r => r.ObterPorId(veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(veiculo);

        await new InativarVeiculo(_veiculos.Object, _unidadeDeTrabalho.Object).Executar(veiculo.Id, CancellationToken.None);

        Assert.False(veiculo.Ativo);
        _unidadeDeTrabalho.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Listar_por_cliente_retorna_veiculos_do_proprietario()
    {
        var cliente = ClienteExistente();
        _veiculos.Setup(r => r.ListarPorProprietario(cliente.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([NovoVeiculo(cliente, "AAA1111"), NovoVeiculo(cliente, "BBB2222")]);
        var caso = new ListarVeiculosDoCliente(_veiculos.Object, _clientes.Object);

        var lista = await caso.Executar(cliente.Id, null, CancellationToken.None);

        Assert.Equal(["AAA1111", "BBB2222"], lista.Select(v => v.Placa));
    }

    [Fact]
    public async Task Listar_por_cliente_inexistente_e_nao_encontrado()
    {
        var caso = new ListarVeiculosDoCliente(_veiculos.Object, _clientes.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            caso.Executar(Guid.NewGuid(), null, CancellationToken.None));
    }
}
