using Moq;
using Oficina.Application.Common;
using Oficina.Application.Demonstracao;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Interfaces;
using Oficina.UnitTests.Domain.Atendimento;

namespace Oficina.UnitTests.Application.Demonstracao;

public class CargaDeDemonstracaoTests
{
    private readonly Mock<IServicoRepository> _servicos = new();
    private readonly Mock<IPecaInsumoRepository> _itens = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IVeiculoRepository> _veiculos = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidade = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.Zero));

    private readonly List<Servico> _servicosAdicionados = [];
    private readonly List<PecaInsumo> _itensAdicionados = [];
    private Cliente? _clienteAdicionado;
    private Veiculo? _veiculoAdicionado;

    public CargaDeDemonstracaoTests()
    {
        _servicos.Setup(r => r.Listar(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _itens.Setup(r => r.Listar(true, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _servicos.Setup(r => r.Adicionar(It.IsAny<Servico>())).Callback<Servico>(_servicosAdicionados.Add);
        _itens.Setup(r => r.Adicionar(It.IsAny<PecaInsumo>())).Callback<PecaInsumo>(_itensAdicionados.Add);
        _clientes.Setup(r => r.Adicionar(It.IsAny<Cliente>())).Callback<Cliente>(c => _clienteAdicionado = c);
        _veiculos.Setup(r => r.Adicionar(It.IsAny<Veiculo>())).Callback<Veiculo>(v => _veiculoAdicionado = v);
    }

    private CargaDeDemonstracao Carga() =>
        new(_servicos.Object, _itens.Object, _clientes.Object, _veiculos.Object, _unidade.Object, _relogio);

    [Fact]
    public async Task Base_vazia_recebe_servicos_peca_insumo_com_saldo_cliente_e_veiculo_mercosul()
    {
        await Carga().Executar(CancellationToken.None);

        Assert.True(_servicosAdicionados.Count >= 3);
        Assert.Contains(_itensAdicionados, i => i.Tipo == TipoPecaInsumo.Peca && i.Saldo > 0);
        Assert.Contains(_itensAdicionados,
            i => i.Tipo == TipoPecaInsumo.Insumo && i.Saldo > 0 && i.Saldo != decimal.Truncate(i.Saldo));
        Assert.NotNull(_clienteAdicionado);
        Assert.Equal(TipoDocumento.Cpf, _clienteAdicionado!.Documento.Tipo);
        Assert.NotNull(_veiculoAdicionado);
        Assert.Equal(_clienteAdicionado.Id, _veiculoAdicionado!.ProprietarioId);
        Assert.Matches("^[A-Z]{3}[0-9][A-Z][0-9]{2}$", _veiculoAdicionado.Placa.Valor);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Base_ja_carregada_nao_duplica_nada()
    {
        await Carga().Executar(CancellationToken.None);
        _servicos.Setup(r => r.Listar(It.IsAny<CancellationToken>())).ReturnsAsync(_servicosAdicionados.ToList());
        _itens.Setup(r => r.Listar(true, It.IsAny<CancellationToken>())).ReturnsAsync(_itensAdicionados.ToList());
        _clientes.Setup(r => r.ObterPorDocumento(It.IsAny<Documento>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_clienteAdicionado);
        _veiculos.Setup(r => r.ObterPorPlaca(It.IsAny<Placa>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_veiculoAdicionado);
        var servicos = _servicosAdicionados.Count;
        var itens = _itensAdicionados.Count;
        var cliente = _clienteAdicionado;
        var veiculo = _veiculoAdicionado;

        await Carga().Executar(CancellationToken.None);

        Assert.Equal(servicos, _servicosAdicionados.Count);
        Assert.Equal(itens, _itensAdicionados.Count);
        Assert.Same(cliente, _clienteAdicionado);
        Assert.Same(veiculo, _veiculoAdicionado);
    }
}
