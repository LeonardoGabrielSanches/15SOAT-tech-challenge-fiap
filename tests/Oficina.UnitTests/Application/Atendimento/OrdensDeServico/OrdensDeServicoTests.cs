using Moq;
using Oficina.Application.Atendimento.OrdensDeServico;
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

namespace Oficina.UnitTests.Application.Atendimento.OrdensDeServico;

public class OrdensDeServicoTests
{
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IVeiculoRepository> _veiculos = new();
    private readonly Mock<IServicoRepository> _servicos = new();
    private readonly Mock<IPecaInsumoRepository> _pecas = new();
    private readonly Mock<IGeradorNumeroOs> _gerador = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidade = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

    private readonly Cliente _cliente;
    private readonly Veiculo _veiculo;

    public OrdensDeServicoTests()
    {
        _cliente = NovoCliente();
        _veiculo = NovoVeiculo(_cliente);
        _clientes.Setup(r => r.ObterPorId(_cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_cliente);
        _veiculos.Setup(r => r.ObterPorId(_veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_veiculo);
        _gerador.Setup(g => g.Proximo(2026, It.IsAny<CancellationToken>())).ReturnsAsync(NumeroOs.Criar(2026, 7));
    }

    private static Cliente NovoCliente(string cpf = "529.982.247-25") =>
        Cliente.Cadastrar("Maria Silva", Documento.Criar(cpf), Email.Criar("maria@oficina.com"), null);

    private Veiculo NovoVeiculo(Cliente proprietario) =>
        Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), proprietario);

    private Servico ServicoExistente(string nome = "Troca de oleo")
    {
        var servico = Servico.Cadastrar(nome, null, Dinheiro.De(100m));
        _servicos.Setup(r => r.ObterPorIds(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                ids.Contains(servico.Id) ? new List<Servico> { servico } : new List<Servico>());
        _servicos.Setup(r => r.Obter(servico.Id, It.IsAny<CancellationToken>())).ReturnsAsync(servico);
        return servico;
    }

    private AbrirOs Abertura() =>
        new(_ordens.Object, _clientes.Object, _veiculos.Object, _servicos.Object, _pecas.Object, _gerador.Object, _unidade.Object, _relogio);

    private PecaInsumo PecaExistente(TipoPecaInsumo tipo = TipoPecaInsumo.Peca, decimal saldo = 10m)
    {
        var item = PecaInsumo.Criar($"Item {Guid.NewGuid():N}", tipo, "un", Dinheiro.De(25m));
        item.RegistrarEntrada(Quantidade.Criar(saldo, tipo));
        _pecas.Setup(r => r.Obter(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        return item;
    }

    private Task<OrdemDeServicoResumoDto> AbrirComPecas(Servico servico, params PecaInsumoNaAbertura[] pecasInsumos) =>
        Abertura().Executar(
            new AbrirOsComando(_cliente.Id, _veiculo.Id, [servico.Id], "atendente-1", pecasInsumos),
            CancellationToken.None);

    private void VerificarQueNadaFoiGravado()
    {
        _ordens.Verify(r => r.Adicionar(It.IsAny<OrdemDeServico>()), Times.Never);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    private Task<OrdemDeServicoResumoDto> Abrir(Guid? clienteId = null, Guid? veiculoId = null, params Guid[] servicoIds) =>
        Abertura().Executar(
            new AbrirOsComando(clienteId ?? _cliente.Id, veiculoId ?? _veiculo.Id, servicoIds, "atendente-1"),
            CancellationToken.None);

    private OrdemDeServico OsRecebida(params Servico[] servicos)
    {
        var os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 1), _cliente, _veiculo, servicos, "atendente-1", _relogio.GetUtcNow());
        _ordens.Setup(r => r.ObterPorId(os.Id, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        return os;
    }


    [Fact]
    public async Task Abrir_cria_os_recebida_com_numero_historico_e_evento()
    {
        var servico = ServicoExistente();
        OrdemDeServico? adicionada = null;
        _ordens.Setup(r => r.Adicionar(It.IsAny<OrdemDeServico>())).Callback<OrdemDeServico>(o => adicionada = o);

        var dto = await Abrir(servicoIds: servico.Id);

        Assert.Equal("OS-2026-00007", dto.Numero);
        Assert.Equal("Recebida", dto.Status);
        Assert.NotNull(adicionada);
        Assert.Equal(StatusOs.Recebida, adicionada.Status);
        Assert.Equal([servico.Id], adicionada.Servicos.Select(s => s.ServicoId));
        var entrada = Assert.Single(adicionada.Historico);
        Assert.Null(entrada.Origem);
        Assert.Equal(StatusOs.Recebida, entrada.Destino);
        Assert.Equal("atendente-1", entrada.Autor);
        Assert.Equal(_relogio.GetUtcNow(), entrada.Momento);
        Assert.Contains(adicionada.EventosDeDominio, e => e is OsAberta a && a.Numero == "OS-2026-00007");
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Abrir_com_pecas_e_insumos_baixa_o_saldo_e_reserva_o_numero_dentro_da_transacao()
    {
        var servico = ServicoExistente();
        var filtro = PecaExistente();
        var oleo = PecaExistente(TipoPecaInsumo.Insumo, 5m);
        OrdemDeServico? adicionada = null;
        _ordens.Setup(r => r.Adicionar(It.IsAny<OrdemDeServico>())).Callback<OrdemDeServico>(o => adicionada = o);
        var ordem = new List<string>();
        _unidade.Setup(u => u.IniciarTransacao(It.IsAny<CancellationToken>())).Callback(() => ordem.Add("transacao")).Returns(Task.CompletedTask);
        _gerador.Setup(g => g.Proximo(2026, It.IsAny<CancellationToken>())).Callback(() => ordem.Add("numero")).ReturnsAsync(NumeroOs.Criar(2026, 7));
        _unidade.Setup(u => u.Confirmar(It.IsAny<CancellationToken>())).Callback(() => ordem.Add("confirmar")).Returns(Task.CompletedTask);

        var dto = await AbrirComPecas(servico, new PecaInsumoNaAbertura(filtro.Id, 2m), new PecaInsumoNaAbertura(oleo.Id, 1.5m));

        Assert.Equal("OS-2026-00007", dto.Numero);
        Assert.NotNull(adicionada);
        Assert.Equal([(filtro.Id, 2m), (oleo.Id, 1.5m)], adicionada.ItensDePecaInsumo.Select(i => (i.PecaInsumoId, i.Quantidade)));
        Assert.Equal((8m, 3.5m), (filtro.Saldo, oleo.Saldo));
        Assert.Equal(["transacao", "numero", "confirmar"], ordem);
    }

    [Fact]
    public async Task Abrir_com_peca_inexistente_e_rejeitado_sem_gravar_nada()
    {
        var servico = ServicoExistente();

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => AbrirComPecas(servico, new PecaInsumoNaAbertura(Guid.NewGuid(), 1m)));

        VerificarQueNadaFoiGravado();
        _gerador.Verify(g => g.Proximo(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Abrir_com_peca_repetida_e_rejeitado_sem_reservar_numero()
    {
        var servico = ServicoExistente();
        var filtro = PecaExistente();

        await Assert.ThrowsAsync<DadosInvalidosException>(() =>
            AbrirComPecas(servico, new PecaInsumoNaAbertura(filtro.Id, 1m), new PecaInsumoNaAbertura(filtro.Id, 1m)));

        VerificarQueNadaFoiGravado();
        _gerador.Verify(g => g.Proximo(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(10m, filtro.Saldo);
    }

    [Fact]
    public async Task Abrir_com_fracao_de_peca_e_rejeitado_e_fracao_de_insumo_e_aceita()
    {
        var servico = ServicoExistente();
        var filtro = PecaExistente();
        var oleo = PecaExistente(TipoPecaInsumo.Insumo);

        await Assert.ThrowsAsync<DadosInvalidosException>(() => AbrirComPecas(servico, new PecaInsumoNaAbertura(filtro.Id, 1.5m)));
        VerificarQueNadaFoiGravado();

        await AbrirComPecas(servico, new PecaInsumoNaAbertura(oleo.Id, 0.25m));
        Assert.Equal(9.75m, oleo.Saldo);
    }

    [Fact]
    public async Task Abrir_com_peca_sem_saldo_ou_inativa_e_rejeitado_sem_confirmar()
    {
        var servico = ServicoExistente();
        var semSaldo = PecaExistente(saldo: 1m);
        var inativa = PecaExistente();
        inativa.Inativar();

        await Assert.ThrowsAsync<DadosInvalidosException>(() => AbrirComPecas(servico, new PecaInsumoNaAbertura(semSaldo.Id, 2m)));
        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => AbrirComPecas(servico, new PecaInsumoNaAbertura(inativa.Id, 1m)));

        VerificarQueNadaFoiGravado();
    }

    [Fact]
    public async Task Abrir_sem_servico_e_rejeitado()
    {
        ServicoExistente();

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Abrir());

        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Abrir_com_servico_repetido_na_lista_e_rejeitado()
    {
        var servico = ServicoExistente();

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Abrir(servicoIds: [servico.Id, servico.Id]));
    }

    [Fact]
    public async Task Abrir_com_cliente_que_nao_e_o_proprietario_e_rejeitado()
    {
        var servico = ServicoExistente();
        var outro = NovoCliente("111.444.777-35");
        _clientes.Setup(r => r.ObterPorId(outro.Id, It.IsAny<CancellationToken>())).ReturnsAsync(outro);

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Abrir(outro.Id, servicoIds: servico.Id));

        _ordens.Verify(r => r.Adicionar(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task Abrir_com_veiculo_que_ja_tem_os_em_andamento_e_rejeitado()
    {
        var servico = ServicoExistente();
        _ordens.Setup(r => r.ExisteEmAndamentoDoVeiculo(_veiculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Abrir(servicoIds: servico.Id));

        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Abrir_com_cliente_inativo_e_rejeitado()
    {
        var servico = ServicoExistente();
        _cliente.Inativar();

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Abrir(servicoIds: servico.Id));
    }

    [Fact]
    public async Task Abrir_com_veiculo_inativo_e_rejeitado()
    {
        var servico = ServicoExistente();
        _veiculo.Inativar();

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Abrir(servicoIds: servico.Id));
    }

    [Fact]
    public async Task Abrir_com_servico_inativo_e_rejeitado()
    {
        var servico = ServicoExistente();
        servico.Inativar();

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Abrir(servicoIds: servico.Id));
    }

    [Fact]
    public async Task Abrir_rejeitada_pelo_agregado_nao_consome_numero_da_os()
    {
        var servico = ServicoExistente();

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Abrir(servicoIds: [servico.Id, servico.Id]));
        _cliente.Inativar();
        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Abrir(servicoIds: servico.Id));

        _gerador.Verify(g => g.Proximo(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _ordens.Verify(r => r.ExisteEmAndamentoDoVeiculo(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Abrir_com_servico_inexistente_e_rejeitado()
    {
        _servicos.Setup(r => r.ObterPorIds(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Servico>());

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => Abrir(servicoIds: Guid.NewGuid()));
    }

    [Fact]
    public async Task Abrir_com_cliente_ou_veiculo_inexistente_e_rejeitado()
    {
        var servico = ServicoExistente();

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => Abrir(clienteId: Guid.NewGuid(), servicoIds: servico.Id));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => Abrir(veiculoId: Guid.NewGuid(), servicoIds: servico.Id));
    }

    [Fact]
    public async Task Incluir_servico_em_os_recebida_adiciona_e_confirma()
    {
        var primeiro = ServicoExistente("Alinhamento");
        var os = OsRecebida(primeiro);
        var segundo = ServicoExistente("Balanceamento");

        await new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object)
            .Executar(os.Id, segundo.Id, PapelDoUsuario.Gerente, CancellationToken.None);

        Assert.Equal([primeiro.Id, segundo.Id], os.Servicos.Select(s => s.ServicoId));
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Incluir_servico_repetido_e_rejeitado()
    {
        var servico = ServicoExistente();
        var os = OsRecebida(servico);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object)
                .Executar(os.Id, servico.Id, PapelDoUsuario.Gerente, CancellationToken.None));
    }

    [Fact]
    public async Task Incluir_servico_inativo_e_rejeitado()
    {
        var existente = ServicoExistente("Alinhamento");
        var os = OsRecebida(existente);
        var inativo = ServicoExistente("Antigo");
        inativo.Inativar();

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object)
                .Executar(os.Id, inativo.Id, PapelDoUsuario.Gerente, CancellationToken.None));
    }

    [Fact]
    public async Task Incluir_servico_fora_de_recebida_e_rejeitado()
    {
        var existente = ServicoExistente("Alinhamento");
        var os = OsRecebida(existente);
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, StatusOs.Aprovada);
        var novo = ServicoExistente("Balanceamento");

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object)
                .Executar(os.Id, novo.Id, PapelDoUsuario.Gerente, CancellationToken.None));
    }

    [Fact]
    public async Task Incluir_servico_em_os_inexistente_ou_servico_inexistente_e_rejeitado()
    {
        var servico = ServicoExistente();
        var os = OsRecebida(servico);
        var caso = new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(Guid.NewGuid(), servico.Id, PapelDoUsuario.Gerente, CancellationToken.None));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => caso.Executar(os.Id, Guid.NewGuid(), PapelDoUsuario.Gerente, CancellationToken.None));
    }

    [Fact]
    public async Task Remover_servico_em_os_recebida_remove_e_confirma()
    {
        var primeiro = ServicoExistente("Alinhamento");
        var segundo = ServicoExistente("Balanceamento");
        var os = OsRecebida(primeiro, segundo);

        await new RemoverServicoDaOs(_ordens.Object, _unidade.Object).Executar(os.Id, primeiro.Id, PapelDoUsuario.Gerente, CancellationToken.None);

        Assert.Equal([segundo.Id], os.Servicos.Select(s => s.ServicoId));
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remover_o_ultimo_servico_e_rejeitado()
    {
        var servico = ServicoExistente();
        var os = OsRecebida(servico);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new RemoverServicoDaOs(_ordens.Object, _unidade.Object).Executar(os.Id, servico.Id, PapelDoUsuario.Gerente, CancellationToken.None));
    }

    [Fact]
    public async Task Remover_servico_que_nao_esta_na_os_e_rejeitado()
    {
        var os = OsRecebida(ServicoExistente());

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            new RemoverServicoDaOs(_ordens.Object, _unidade.Object).Executar(os.Id, Guid.NewGuid(), PapelDoUsuario.Gerente, CancellationToken.None));
    }

    [Fact]
    public async Task Remover_servico_fora_de_recebida_e_rejeitado()
    {
        var primeiro = ServicoExistente("Alinhamento");
        var segundo = ServicoExistente("Balanceamento");
        var os = OsRecebida(primeiro, segundo);
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, StatusOs.Aprovada);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            new RemoverServicoDaOs(_ordens.Object, _unidade.Object).Executar(os.Id, primeiro.Id, PapelDoUsuario.Gerente, CancellationToken.None));
    }

    [Fact]
    public async Task Listar_consulta_a_pagina_pedida_com_o_filtro_de_status()
    {
        var consulta = new Mock<IConsultaDeOrdensDeServico>();
        var esperada = new PaginaDeOrdensDeServicoDto([], 3, 50, 120);
        consulta.Setup(c => c.Listar(StatusOs.Finalizada, 3, 50, It.IsAny<CancellationToken>())).ReturnsAsync(esperada);

        var pagina = await new ListarOs(consulta.Object).Executar(StatusOs.Finalizada, 3, 50, CancellationToken.None);

        Assert.Same(esperada, pagina);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_rejeita_pagina_ou_tamanho_fora_dos_limites_sem_consultar(int pagina, int tamanho)
    {
        var consulta = new Mock<IConsultaDeOrdensDeServico>();

        await Assert.ThrowsAsync<DadosInvalidosException>(() =>
            new ListarOs(consulta.Object).Executar(null, pagina, tamanho, CancellationToken.None));

        consulta.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Detalhar_traz_cliente_veiculo_servicos_versoes_e_historico()
    {
        var servico = ServicoExistente();
        var os = OsRecebida(servico);

        var detalhe = await new DetalharOs(_ordens.Object, _clientes.Object, _veiculos.Object, _servicos.Object, _pecas.Object)
            .Executar(os.Id, CancellationToken.None);

        Assert.Equal("OS-2026-00001", detalhe.Numero);
        Assert.Equal(_cliente.Id, detalhe.Cliente.Id);
        Assert.Equal(_veiculo.Id, detalhe.Veiculo.Id);
        var item = Assert.Single(detalhe.Servicos);
        Assert.Equal((servico.Id, "Troca de oleo", 100m), (item.ServicoId, item.Nome, item.Preco));
        Assert.Empty(detalhe.VersoesDoOrcamento);
        var historico = Assert.Single(detalhe.Historico);
        Assert.Equal((null, "Recebida", "atendente-1"), (historico.Origem, historico.Destino, historico.Autor));
    }

    [Fact]
    public async Task Detalhar_os_inexistente_e_rejeitado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            new DetalharOs(_ordens.Object, _clientes.Object, _veiculos.Object, _servicos.Object, _pecas.Object)
                .Executar(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Detalhar_expoe_itens_congelados_motivo_da_recusa_e_precos_congelados_dos_servicos()
    {
        var servico = ServicoExistente();
        var os = OsRecebida(servico);
        var agora = _relogio.GetUtcNow();
        os.IniciarDiagnostico("m", agora);
        os.ConcluirDiagnostico([servico], [], "m", agora);
        os.RecusarOrcamento(os.VersaoVigente!.Token, "Muito caro", "Cliente", agora);
        servico.Atualizar(servico.Nome, null, Dinheiro.De(150m));

        var detalhe = await new DetalharOs(_ordens.Object, _clientes.Object, _veiculos.Object, _servicos.Object, _pecas.Object)
            .Executar(os.Id, CancellationToken.None);

        var versao = Assert.Single(detalhe.VersoesDoOrcamento);
        Assert.Equal("Muito caro", versao.MotivoDaRecusa);
        var item = Assert.Single(versao.Itens);
        Assert.Equal(("Servico", servico.Id, "Troca de oleo", 1m, 100m, 100m), (item.Tipo, item.ReferenciaId, item.Descricao, item.Quantidade, item.PrecoUnitario, item.Subtotal));
        Assert.Equal(100m, Assert.Single(detalhe.Servicos).Preco);
    }

    [Fact]
    public async Task Detalhar_usa_preco_do_catalogo_para_servico_fora_da_versao_vigente()
    {
        var primeiro = ServicoExistente();
        var os = OsRecebida(primeiro);
        var agora = _relogio.GetUtcNow();
        os.IniciarDiagnostico("m", agora);
        os.ConcluirDiagnostico([primeiro], [], "m", agora);
        os.RecusarOrcamento(os.VersaoVigente!.Token, null, "Cliente", agora);
        var segundo = Servico.Cadastrar("Balanceamento", null, Dinheiro.De(80m));
        os.IncluirServico(segundo);
        _servicos.Setup(r => r.ObterPorIds(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Servico> { primeiro, segundo });
        primeiro.Atualizar(primeiro.Nome, null, Dinheiro.De(999m));

        var detalhe = await new DetalharOs(_ordens.Object, _clientes.Object, _veiculos.Object, _servicos.Object, _pecas.Object)
            .Executar(os.Id, CancellationToken.None);

        Assert.Equal([("Balanceamento", 80m), ("Troca de oleo", 100m)], detalhe.Servicos.Select(x => (x.Nome, x.Preco)));
    }

    [Theory]
    [InlineData(StatusOs.Recebida, PapelDoUsuario.Mecanico)]
    [InlineData(StatusOs.EmDiagnostico, PapelDoUsuario.Atendente)]
    public async Task Incluir_e_remover_servico_com_papel_errado_para_o_status_e_negado_e_nao_confirma(StatusOs status, PapelDoUsuario papel)
    {
        var primeiro = ServicoExistente("Alinhamento");
        var os = OsRecebida(primeiro);
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, status);
        var segundo = ServicoExistente("Balanceamento");

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object).Executar(os.Id, segundo.Id, papel, CancellationToken.None));
        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            new RemoverServicoDaOs(_ordens.Object, _unidade.Object).Executar(os.Id, primeiro.Id, papel, CancellationToken.None));

        Assert.Single(os.Servicos);
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(StatusOs.Recebida, PapelDoUsuario.Atendente)]
    [InlineData(StatusOs.EmDiagnostico, PapelDoUsuario.Mecanico)]
    [InlineData(StatusOs.Recebida, PapelDoUsuario.Gerente)]
    [InlineData(StatusOs.EmDiagnostico, PapelDoUsuario.Gerente)]
    [InlineData(StatusOs.Recebida, PapelDoUsuario.Atendente | PapelDoUsuario.Mecanico)]
    public async Task Incluir_servico_com_papel_correto_para_o_status_e_permitido(StatusOs status, PapelDoUsuario papel)
    {
        var primeiro = ServicoExistente("Alinhamento");
        var os = OsRecebida(primeiro);
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, status);
        var segundo = ServicoExistente("Balanceamento");

        await new IncluirServicoNaOs(_ordens.Object, _servicos.Object, _unidade.Object).Executar(os.Id, segundo.Id, papel, CancellationToken.None);

        Assert.Equal(2, os.Servicos.Count);
    }
}
