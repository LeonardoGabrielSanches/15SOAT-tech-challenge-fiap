using Moq;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;
using Oficina.UnitTests.Domain.Atendimento;

namespace Oficina.UnitTests.Application.Atendimento;

public class CasosDeUsoDeCancelamentoTests
{
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<IPecaInsumoRepository> _pecas = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidade = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
    private readonly Servico _servico = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));
    private readonly Cliente _cliente;
    private readonly Veiculo _veiculo;

    public CasosDeUsoDeCancelamentoTests()
    {
        _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
        _veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), _cliente);
    }

    private OrdemDeServico Os(StatusOs status = StatusOs.Recebida)
    {
        var os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 1), _cliente, _veiculo, [_servico], "atendente-1", _relogio.GetUtcNow());
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, status);
        _ordens.Setup(r => r.ObterPorId(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        return os;
    }

    private PecaInsumo Peca(decimal saldo = 10m)
    {
        var item = PecaInsumo.Criar("Filtro", TipoPecaInsumo.Peca, "un", Dinheiro.De(25m));
        item.RegistrarEntrada(Quantidade.Criar(saldo, TipoPecaInsumo.Peca));
        _pecas.Setup(r => r.Obter(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        return item;
    }

    private Task Cancelar(OrdemDeServico os, string motivo = "Cliente desistiu", string autor = "atendente-1") =>
        new CancelarOs(_ordens.Object, _pecas.Object, _unidade.Object, _relogio)
            .Executar(os.Id, motivo, autor, CancellationToken.None);

    [Theory]
    [InlineData(StatusOs.Recebida)]
    [InlineData(StatusOs.EmDiagnostico)]
    [InlineData(StatusOs.AguardandoAprovacao)]
    public async Task Cancela_nos_status_permitidos_com_historico_evento_e_confirma(StatusOs status)
    {
        var os = Os(status);

        await Cancelar(os, "Cliente desistiu", "atendente-2");

        Assert.Equal(StatusOs.Cancelada, os.Status);
        var entrada = os.Historico[^1];
        Assert.Equal((status, StatusOs.Cancelada, "atendente-2"), (entrada.Origem, entrada.Destino, entrada.Autor));
        var evento = Assert.Single(os.EventosDeDominio.OfType<OsCancelada>());
        Assert.Equal(("Cliente desistiu", os.Id), (evento.Motivo, evento.OsId));
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(StatusOs.Aprovada)]
    [InlineData(StatusOs.EmExecucao)]
    [InlineData(StatusOs.Finalizada)]
    [InlineData(StatusOs.Entregue)]
    [InlineData(StatusOs.Cancelada)]
    public async Task Cancelar_fora_dos_status_permitidos_e_conflito(StatusOs status)
    {
        var os = Os(status);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Cancelar(os));

        Assert.Equal(status, os.Status);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Motivo_obrigatorio(string motivo)
    {
        var os = Os();

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Cancelar(os, motivo));

        Assert.Equal(StatusOs.Recebida, os.Status);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Os_inexistente_e_404()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            new CancelarOs(_ordens.Object, _pecas.Object, _unidade.Object, _relogio)
                .Executar(Guid.NewGuid(), "motivo", "a", CancellationToken.None));
    }

    [Fact]
    public async Task Estorna_todas_as_pecas_ao_saldo_e_esvazia_os_itens()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var filtro = Peca(10m);
        var vela = Peca(8m);
        os.IncluirPecaInsumo(filtro, Quantidade.Criar(3m, TipoPecaInsumo.Peca), _relogio.GetUtcNow());
        os.IncluirPecaInsumo(vela, Quantidade.Criar(4m, TipoPecaInsumo.Peca), _relogio.GetUtcNow());

        await Cancelar(os);

        Assert.Equal(10m, filtro.Saldo);
        Assert.Equal(8m, vela.Saldo);
        Assert.Empty(os.ItensDePecaInsumo);
    }

    [Fact]
    public async Task Invalida_o_token_da_versao_pendente()
    {
        var os = Os(StatusOs.EmDiagnostico);
        os.ConcluirDiagnostico([_servico], [], "mecanico-1", _relogio.GetUtcNow());
        Assert.Equal(SituacaoDaVersao.Pendente, os.VersaoVigente!.Situacao);

        await Cancelar(os);

        Assert.NotEqual(SituacaoDaVersao.Pendente, os.VersaoVigente!.Situacao);
    }
}
