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

public class CasosDeUsoDeAprovacaoTests
{
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<IUnidadeDeTrabalho> _unidade = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
    private readonly Servico _servico = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));
    private readonly Cliente _cliente;
    private readonly Veiculo _veiculo;

    public CasosDeUsoDeAprovacaoTests()
    {
        _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
        _veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), _cliente);
    }

    private OrdemDeServico OsAguardandoAprovacao()
    {
        var os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 1), _cliente, _veiculo, [_servico], "atendente-1", _relogio.GetUtcNow());
        os.IniciarDiagnostico("mecanico-1", _relogio.GetUtcNow());
        Concluir(os);
        return os;
    }

    private void Concluir(OrdemDeServico os) =>
        os.ConcluirDiagnostico([_servico], [], "mecanico-1", _relogio.GetUtcNow());

    private string Registrar(OrdemDeServico os)
    {
        var token = os.VersaoVigente!.Token;
        _ordens.Setup(r => r.ObterPorTokenDoOrcamento(token, It.IsAny<CancellationToken>())).ReturnsAsync(os);
        return token;
    }

    private Task Aprovar(string token) =>
        new AprovarOrcamento(_ordens.Object, _unidade.Object, _relogio).Executar(token, CancellationToken.None);

    private Task Recusar(string token, string? motivo = null) =>
        new RecusarOrcamento(_ordens.Object, _unidade.Object, _relogio).Executar(token, motivo, CancellationToken.None);

    [Fact]
    public async Task Aprovar_move_para_aprovada_com_autor_cliente_evento_e_confirma()
    {
        var os = OsAguardandoAprovacao();
        var token = Registrar(os);

        await Aprovar(token);

        Assert.Equal(StatusOs.Aprovada, os.Status);
        Assert.Equal(SituacaoDaVersao.Aprovada, os.VersaoVigente!.Situacao);
        var entrada = os.Historico[^1];
        Assert.Equal((StatusOs.AguardandoAprovacao, StatusOs.Aprovada, "Cliente"), (entrada.Origem, entrada.Destino, entrada.Autor));
        Assert.Contains(os.EventosDeDominio, e => e is OrcamentoAprovado { Versao: 1 });
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Recusar_volta_para_em_diagnostico_guarda_motivo_e_registra_evento()
    {
        var os = OsAguardandoAprovacao();
        var token = Registrar(os);

        await Recusar(token, "Muito caro");

        Assert.Equal(StatusOs.EmDiagnostico, os.Status);
        Assert.Equal(SituacaoDaVersao.Recusada, os.VersaoVigente!.Situacao);
        Assert.Equal("Muito caro", os.VersaoVigente.MotivoDaRecusa);
        Assert.Equal("Muito caro", os.MotivoDaUltimaRecusa);
        var entrada = os.Historico[^1];
        Assert.Equal((StatusOs.AguardandoAprovacao, StatusOs.EmDiagnostico, "Cliente"), (entrada.Origem, entrada.Destino, entrada.Autor));
        Assert.Contains(os.EventosDeDominio, e => e is OrcamentoRecusado { Versao: 1, Motivo: "Muito caro" });
        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Recusar_sem_motivo_e_aceito()
    {
        var os = OsAguardandoAprovacao();
        var token = Registrar(os);

        await Recusar(token, "  ");

        Assert.Equal(StatusOs.EmDiagnostico, os.Status);
        Assert.Null(os.MotivoDaUltimaRecusa);
    }

    [Fact]
    public async Task Recusar_com_motivo_longo_demais_e_rejeitado()
    {
        var os = OsAguardandoAprovacao();
        var token = Registrar(os);

        await Assert.ThrowsAsync<DadosInvalidosException>(() => Recusar(token, new string('x', 501)));

        Assert.Equal(StatusOs.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public async Task Token_ja_usado_na_aprovacao_e_conflito()
    {
        var os = OsAguardandoAprovacao();
        var token = Registrar(os);
        await Aprovar(token);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Aprovar(token));
        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Recusar(token));

        _unidade.Verify(u => u.Confirmar(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Token_ja_usado_na_recusa_e_conflito()
    {
        var os = OsAguardandoAprovacao();
        var token = Registrar(os);
        await Recusar(token);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Aprovar(token));
    }

    [Fact]
    public async Task Token_de_versao_superada_e_conflito_e_o_novo_token_funciona()
    {
        var os = OsAguardandoAprovacao();
        var antigo = Registrar(os);
        await Recusar(antigo);
        Concluir(os);
        var novo = Registrar(os);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Aprovar(antigo));
        await Aprovar(novo);

        Assert.Equal(StatusOs.Aprovada, os.Status);
        Assert.NotEqual(antigo, novo);
    }

    [Fact]
    public async Task Token_de_versao_pendente_superada_por_nova_conclusao_e_conflito()
    {
        var os = OsAguardandoAprovacao();
        var antigo = Registrar(os);
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, StatusOs.EmDiagnostico);
        Concluir(os);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Aprovar(antigo));
        Assert.Equal(StatusOs.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public async Task Token_de_os_cancelada_e_conflito()
    {
        var os = OsAguardandoAprovacao();
        var token = Registrar(os);
        typeof(OrdemDeServico).GetProperty(nameof(OrdemDeServico.Status))!.SetValue(os, StatusOs.Cancelada);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() => Aprovar(token));
    }

    [Fact]
    public async Task Token_desconhecido_e_404()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => Aprovar("inexistente"));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => Recusar("inexistente"));
    }
}
