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
using Oficina.UnitTests.Domain.Atendimento;

namespace Oficina.UnitTests.Application.Atendimento;

public class CasosDeUsoDeExecucaoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidade = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(Agora);
    private readonly Servico _servico = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));
    private readonly Cliente _cliente;
    private readonly Veiculo _veiculo;

    public CasosDeUsoDeExecucaoTests()
    {
        _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
        _veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), _cliente);
    }

    private OrdemDeServico OsAprovada(DateTimeOffset? abertaEm = null)
    {
        var os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 1), _cliente, _veiculo, [_servico], "atendente-1", abertaEm ?? Agora);
        os.IniciarDiagnostico("mecanico-1", Agora);
        os.ConcluirDiagnostico([_servico], [], "mecanico-1", Agora);
        os.AprovarOrcamento(os.VersaoVigente!.Token, "Cliente", Agora);
        return os;
    }

    private OrdemDeServico Registrar(OrdemDeServico os)
    {
        _ordens.Setup(r => r.ObterPorId(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        return os;
    }

    [Fact]
    public async Task Iniciar_execucao_move_para_em_execucao_com_autor_e_momento_e_confirma()
    {
        var os = Registrar(OsAprovada());

        await new IniciarExecucao(_ordens.Object, _unidade.Object, _relogio).Executar(os.Id, "mecanico-2", CancellationToken.None);

        Assert.Equal(StatusOs.EmExecucao, os.Status);
        var ultima = os.Historico[^1];
        Assert.Equal(StatusOs.Aprovada, ultima.Origem);
        Assert.Equal(StatusOs.EmExecucao, ultima.Destino);
        Assert.Equal("mecanico-2", ultima.Autor);
        Assert.Equal(Agora, ultima.Momento);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Iniciar_execucao_fora_de_ordem_e_conflito_e_nao_confirma()
    {
        var os = Registrar(OsAprovada());
        os.IniciarExecucao("m", Agora);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new IniciarExecucao(_ordens.Object, _unidade.Object, _relogio).Executar(os.Id, "m", CancellationToken.None));

        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Finalizar_execucao_move_para_finalizada()
    {
        var os = Registrar(OsAprovada());
        os.IniciarExecucao("m", Agora);

        await new FinalizarExecucao(_ordens.Object, _unidade.Object, _relogio).Executar(os.Id, "mecanico-2", CancellationToken.None);

        Assert.Equal(StatusOs.Finalizada, os.Status);
        Assert.Equal("mecanico-2", os.Historico[^1].Autor);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Finalizar_execucao_sem_ter_iniciado_e_conflito()
    {
        var os = Registrar(OsAprovada());

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new FinalizarExecucao(_ordens.Object, _unidade.Object, _relogio).Executar(os.Id, "m", CancellationToken.None));
    }

    [Fact]
    public async Task Registrar_entrega_move_para_entregue()
    {
        var os = Registrar(OsAprovada());
        os.IniciarExecucao("m", Agora);
        os.FinalizarExecucao("m", Agora);

        await new RegistrarEntrega(_ordens.Object, _unidade.Object, _relogio).Executar(os.Id, "atendente-2", CancellationToken.None);

        Assert.Equal(StatusOs.Entregue, os.Status);
        Assert.Equal("atendente-2", os.Historico[^1].Autor);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Registrar_entrega_de_os_nao_finalizada_e_conflito()
    {
        var os = Registrar(OsAprovada());
        os.IniciarExecucao("m", Agora);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new RegistrarEntrega(_ordens.Object, _unidade.Object, _relogio).Executar(os.Id, "a", CancellationToken.None));
    }

    [Fact]
    public void Entregue_e_final()
    {
        var os = OsAprovada();
        os.IniciarExecucao("m", Agora);
        os.FinalizarExecucao("m", Agora);
        os.RegistrarEntrega("a", Agora);

        Assert.Throws<ConflitoDeEstadoException>(() => os.RegistrarEntrega("a", Agora));
        Assert.Throws<ConflitoDeEstadoException>(() => os.IniciarExecucao("m", Agora));
        Assert.Throws<ConflitoDeEstadoException>(() => os.FinalizarExecucao("m", Agora));
    }

    [Fact]
    public async Task Fila_lista_aprovadas_da_mais_antiga_para_a_mais_nova()
    {
        var nova = OsAprovada(Agora.AddHours(-1));
        var antiga = OsAprovada(Agora.AddDays(-2));
        var media = OsAprovada(Agora.AddDays(-1));
        _ordens.Setup(r => r.Listar(StatusOs.Aprovada, It.IsAny<CancellationToken>()))
            .ReturnsAsync([nova, antiga, media]);

        var fila = await new ListarFilaDeExecucao(_ordens.Object).Executar(CancellationToken.None);

        Assert.Equal([antiga.Id, media.Id, nova.Id], fila.Select(o => o.Id));
    }

    [Fact]
    public void Transicoes_de_execucao_e_entrega_registram_eventos_de_dominio()
    {
        var os = OsAprovada();
        os.LimparEventos();

        os.IniciarExecucao("m", Agora);
        os.FinalizarExecucao("m", Agora);
        os.RegistrarEntrega("a", Agora);

        Assert.Contains(os.EventosDeDominio, e => e is ExecucaoIniciada x && x.OsId == os.Id && x.Numero == os.Numero.Valor && x.OcorridoEm == Agora);
        Assert.Contains(os.EventosDeDominio, e => e is ExecucaoFinalizada x && x.OsId == os.Id && x.OcorridoEm == Agora);
        Assert.Contains(os.EventosDeDominio, e => e is EntregaRealizada x && x.OsId == os.Id && x.OcorridoEm == Agora);
    }
}
