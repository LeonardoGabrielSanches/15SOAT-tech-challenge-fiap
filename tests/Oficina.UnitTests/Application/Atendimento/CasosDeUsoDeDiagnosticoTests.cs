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
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;
using Oficina.UnitTests.Domain.Atendimento;

namespace Oficina.UnitTests.Application.Atendimento;

public class CasosDeUsoDeDiagnosticoTests
{
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<IServicoRepository> _servicos = new();
    private readonly Mock<IPecaInsumoRepository> _pecas = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidade = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
    private readonly Servico _servico = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));
    private readonly Cliente _cliente;
    private readonly Veiculo _veiculo;

    public CasosDeUsoDeDiagnosticoTests()
    {
        _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
        _veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), _cliente);
        _servicos.Setup(r => r.Obter(_servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_servico);
    }

    private OrdemDeServico Os(StatusOs status = StatusOs.Recebida)
    {
        var os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 1), _cliente, _veiculo, [_servico], "atendente-1", _relogio.GetUtcNow());
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, status);
        _ordens.Setup(r => r.ObterPorId(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        return os;
    }

    private PecaInsumo Peca(TipoPecaInsumo tipo = TipoPecaInsumo.Peca, decimal saldo = 10m)
    {
        var item = PecaInsumo.Criar("Filtro", tipo, "un", Dinheiro.De(25m));
        item.RegistrarEntrada(Quantidade.Criar(saldo, tipo));
        _pecas.Setup(r => r.Obter(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        return item;
    }

    private Task Incluir(OrdemDeServico os, PecaInsumo peca, decimal quantidade) =>
        new IncluirPecaInsumoNaOs(_ordens.Object, _pecas.Object, _unidade.Object, _relogio)
            .Executar(os.Id, peca.Id, quantidade, CancellationToken.None);

    private Task Remover(OrdemDeServico os, PecaInsumo peca, decimal? quantidade = null) =>
        new RemoverPecaInsumoDaOs(_ordens.Object, _pecas.Object, _unidade.Object)
            .Executar(os.Id, peca.Id, quantidade, CancellationToken.None);

    [Fact]
    public async Task Iniciar_diagnostico_move_para_em_diagnostico_registra_historico_e_confirma()
    {
        var os = Os();

        await new IniciarDiagnostico(_ordens.Object, _unidade.Object, _relogio)
            .Executar(os.Id, "mecanico-1", CancellationToken.None);

        Assert.Equal(StatusOs.EmDiagnostico, os.Status);
        var entrada = os.Historico[^1];
        Assert.Equal((StatusOs.Recebida, StatusOs.EmDiagnostico, "mecanico-1"), (entrada.Origem, entrada.Destino, entrada.Autor));
        Assert.Equal(_relogio.GetUtcNow(), entrada.Momento);
        Assert.Contains(os.EventosDeDominio, e => e is DiagnosticoIniciado);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(StatusOs.EmDiagnostico)]
    [InlineData(StatusOs.AguardandoAprovacao)]
    [InlineData(StatusOs.Aprovada)]
    [InlineData(StatusOs.Cancelada)]
    public async Task Iniciar_diagnostico_fora_de_recebida_e_conflito(StatusOs status)
    {
        var os = Os(status);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new IniciarDiagnostico(_ordens.Object, _unidade.Object, _relogio).Executar(os.Id, "m", CancellationToken.None));

        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Iniciar_diagnostico_de_os_inexistente_e_404()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            new IniciarDiagnostico(_ordens.Object, _unidade.Object, _relogio).Executar(Guid.NewGuid(), "m", CancellationToken.None));
    }

    [Fact]
    public async Task Incluir_peca_da_baixa_no_saldo_e_adiciona_o_item()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 10m);

        await Incluir(os, peca, 3m);

        Assert.Equal(7m, peca.Saldo);
        var item = Assert.Single(os.ItensDePecaInsumo);
        Assert.Equal((peca.Id, 3m), (item.PecaInsumoId, item.Quantidade));
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Incluir_item_ja_presente_soma_a_quantidade_e_baixa_a_diferenca()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 10m);

        await Incluir(os, peca, 3m);
        await Incluir(os, peca, 2m);

        Assert.Equal(5m, peca.Saldo);
        Assert.Equal(5m, Assert.Single(os.ItensDePecaInsumo).Quantidade);
    }

    [Fact]
    public async Task Incluir_com_saldo_insuficiente_e_422_sem_alterar_saldo_nem_os()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 2m);

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Incluir(os, peca, 3m));

        Assert.Equal(2m, peca.Saldo);
        Assert.Empty(os.ItensDePecaInsumo);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Incluir_insumo_aceita_fracao_e_peca_nao()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var oleo = Peca(TipoPecaInsumo.Insumo, 20m);
        var filtro = Peca(TipoPecaInsumo.Peca, 5m);

        await Incluir(os, oleo, 4.5m);
        await Assert.ThrowsAsync<DadosInvalidosException>(() => Incluir(os, filtro, 1.5m));

        Assert.Equal(15.5m, oleo.Saldo);
        Assert.Equal(5m, filtro.Saldo);
    }

    [Theory]
    [InlineData(StatusOs.Recebida)]
    [InlineData(StatusOs.AguardandoAprovacao)]
    [InlineData(StatusOs.Aprovada)]
    [InlineData(StatusOs.EmExecucao)]
    [InlineData(StatusOs.Cancelada)]
    public async Task Incluir_e_remover_peca_fora_de_em_diagnostico_e_conflito_sem_mexer_no_saldo(StatusOs status)
    {
        var os = Os(status);
        var peca = Peca(saldo: 10m);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Incluir(os, peca, 1m));
        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Remover(os, peca));

        Assert.Equal(10m, peca.Saldo);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Incluir_peca_inativa_e_rejeitado()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca();
        peca.Inativar();

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Incluir(os, peca, 1m));

        Assert.Empty(os.ItensDePecaInsumo);
    }

    [Fact]
    public async Task Incluir_com_os_ou_peca_inexistente_e_404()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca();
        var caso = new IncluirPecaInsumoNaOs(_ordens.Object, _pecas.Object, _unidade.Object, _relogio);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(Guid.NewGuid(), peca.Id, 1m, CancellationToken.None));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(os.Id, Guid.NewGuid(), 1m, CancellationToken.None));
    }

    [Fact]
    public async Task Remover_parcialmente_estorna_a_quantidade_e_mantem_a_diferenca()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 10m);
        await Incluir(os, peca, 5m);

        await Remover(os, peca, 2m);

        Assert.Equal(7m, peca.Saldo);
        Assert.Equal(3m, Assert.Single(os.ItensDePecaInsumo).Quantidade);
    }

    [Fact]
    public async Task Remover_totalmente_sem_informar_quantidade_estorna_tudo_e_retira_a_linha()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 10m);
        await Incluir(os, peca, 5m);

        await Remover(os, peca);

        Assert.Equal(10m, peca.Saldo);
        Assert.Empty(os.ItensDePecaInsumo);
    }

    [Fact]
    public async Task Remover_a_quantidade_inteira_retira_a_linha()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 10m);
        await Incluir(os, peca, 5m);

        await Remover(os, peca, 5m);

        Assert.Equal(10m, peca.Saldo);
        Assert.Empty(os.ItensDePecaInsumo);
    }

    [Fact]
    public async Task Remover_mais_do_que_o_incluido_e_422_e_item_ausente_e_404()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 10m);
        var outra = Peca(saldo: 10m);
        await Incluir(os, peca, 2m);

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Remover(os, peca, 3m));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => Remover(os, outra));

        Assert.Equal(8m, peca.Saldo);
        Assert.Equal(10m, outra.Saldo);
    }

    [Fact]
    public async Task Remover_peca_inativa_ainda_estorna()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 10m);
        await Incluir(os, peca, 4m);
        peca.Inativar();

        await Remover(os, peca);

        Assert.Equal(10m, peca.Saldo);
    }

    [Fact]
    public async Task Detalhar_traz_as_pecas_e_insumos_da_os_e_nenhum_motivo_de_recusa()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var peca = Peca(saldo: 10m);
        await Incluir(os, peca, 3m);
        var clientes = new Mock<IClienteRepository>();
        clientes.Setup(r => r.ObterPorId(_cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_cliente);
        var veiculos = new Mock<IVeiculoRepository>();
        veiculos.Setup(r => r.ObterPorId(_veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_veiculo);
        _servicos.Setup(r => r.ObterPorIds(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Servico> { _servico });

        var detalhe = await new DetalharOs(_ordens.Object, clientes.Object, veiculos.Object, _servicos.Object, _pecas.Object)
            .Executar(os.Id, CancellationToken.None);

        var item = Assert.Single(detalhe.PecasInsumos);
        Assert.Equal((peca.Id, "Filtro", 3m, 25m), (item.PecaInsumoId, item.Nome, item.Quantidade, item.Preco));
        Assert.Null(detalhe.MotivoDaUltimaRecusa);
    }

    [Fact]
    public async Task Mecanico_inclui_e_remove_servico_em_em_diagnostico()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var novo = Servico.Cadastrar("Alinhamento", null, Dinheiro.De(80m));
        _servicos.Setup(r => r.Obter(novo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(novo);

        await new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object).Executar(os.Id, novo.Id, PapelDoUsuario.Mecanico, CancellationToken.None);
        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object).Executar(os.Id, novo.Id, PapelDoUsuario.Mecanico, CancellationToken.None));
        await new RemoverServicoDaOs(_ordens.Object, _unidade.Object).Executar(os.Id, _servico.Id, PapelDoUsuario.Mecanico, CancellationToken.None);

        Assert.Equal([novo.Id], os.Servicos.Select(s => s.ServicoId));
    }

    private Task Concluir(OrdemDeServico os, string autor = "mecanico-1") =>
        new ConcluirDiagnostico(_ordens.Object, _servicos.Object, _pecas.Object, _unidade.Object, _relogio)
            .Executar(os.Id, autor, CancellationToken.None);

    private void Catalogo(params Servico[] servicos) =>
        _servicos.Setup(r => r.ObterPorIds(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(servicos);

    [Fact]
    public async Task Concluir_gera_versao_com_total_historico_e_evento()
    {
        var os = Os(StatusOs.EmDiagnostico);
        Catalogo(_servico);
        var filtro = Peca(saldo: 10m);
        await Incluir(os, filtro, 3m);

        await Concluir(os);

        Assert.Equal(StatusOs.AguardandoAprovacao, os.Status);
        var versao = Assert.Single(os.Versoes);
        Assert.Equal((1, 175m, SituacaoDaVersao.Pendente), (versao.Numero, versao.Total, versao.Situacao));
        Assert.Equal(2, versao.Itens.Count);
        Assert.NotEmpty(versao.Token);
        var entrada = os.Historico[^1];
        Assert.Equal((StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao, "mecanico-1"), (entrada.Origem, entrada.Destino, entrada.Autor));
        var evento = Assert.Single(os.EventosDeDominio.OfType<DiagnosticoConcluido>());
        Assert.Equal((175m, versao.Token, 1), (evento.Total, evento.Token, evento.Versao));
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Mudar_preco_no_catalogo_depois_da_geracao_nao_altera_a_versao()
    {
        var os = Os(StatusOs.EmDiagnostico);
        Catalogo(_servico);
        var filtro = Peca(saldo: 10m);
        await Incluir(os, filtro, 2m);
        await Concluir(os);

        _servico.Atualizar("Troca de oleo", null, Dinheiro.De(999m));
        filtro.Atualizar("Filtro", "un", Dinheiro.De(999m));

        var versao = os.Versoes[0];
        Assert.Equal(150m, versao.Total);
        Assert.Equal([100m, 25m], versao.Itens.Select(i => i.PrecoUnitario));
        Assert.Equal([1m, 2m], versao.Itens.Select(i => i.Quantidade));
    }

    [Fact]
    public async Task Nova_conclusao_cria_nova_versao_e_supera_o_token_anterior()
    {
        var os = Os(StatusOs.EmDiagnostico);
        Catalogo(_servico);
        await Concluir(os);
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, StatusOs.EmDiagnostico);

        await Concluir(os);

        Assert.Equal([1, 2], os.Versoes.Select(v => v.Numero));
        Assert.Equal([SituacaoDaVersao.Superada, SituacaoDaVersao.Pendente], os.Versoes.Select(v => v.Situacao));
        Assert.NotEqual(os.Versoes[0].Token, os.Versoes[1].Token);
        Assert.Same(os.Versoes[1], os.VersaoVigente);
    }

    [Theory]
    [InlineData(StatusOs.Recebida)]
    [InlineData(StatusOs.AguardandoAprovacao)]
    [InlineData(StatusOs.Aprovada)]
    [InlineData(StatusOs.Cancelada)]
    public async Task Concluir_fora_de_em_diagnostico_e_conflito(StatusOs status)
    {
        var os = Os(status);
        Catalogo(_servico);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Concluir(os));

        Assert.Empty(os.Versoes);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Concluir_sem_servico_e_rejeitado()
    {
        var os = Os(StatusOs.EmDiagnostico);
        var campo = typeof(OrdemDeServico).GetField("_servicos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<ServicoDaOs>)campo.GetValue(os)!).Clear();
        Catalogo();

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Concluir(os));

        Assert.Equal(StatusOs.EmDiagnostico, os.Status);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Concluir_os_inexistente_e_404()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            new ConcluirDiagnostico(_ordens.Object, _servicos.Object, _pecas.Object, _unidade.Object, _relogio)
                .Executar(Guid.NewGuid(), "m", CancellationToken.None));
    }

    [Fact]
    public async Task Handler_envia_o_orcamento_ao_email_do_cliente_pelo_notificador()
    {
        var clientes = new Mock<IClienteRepository>();
        clientes.Setup(r => r.ObterPorId(_cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_cliente);
        var notificador = new Mock<INotificador>();
        var evento = new DiagnosticoConcluido(Guid.NewGuid(), "OS-2026-00001", _cliente.Id, 1, 175m, "TOKEN123", _relogio.GetUtcNow());

        await new EnviarOrcamentoAoConcluirDiagnostico(clientes.Object, notificador.Object).Manipular(evento, CancellationToken.None);

        notificador.Verify(n => n.EnviarOrcamento("maria@oficina.com", "OS-2026-00001", 175m, "TOKEN123", It.IsAny<CancellationToken>()), Times.Once);
    }
}
