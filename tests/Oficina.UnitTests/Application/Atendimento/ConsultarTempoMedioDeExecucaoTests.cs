using Moq;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.UnitTests.Application.Atendimento;

public class ConsultarTempoMedioDeExecucaoTests
{
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<IServicoRepository> _servicos = new();
    private readonly List<Servico> _catalogo = [];

    public ConsultarTempoMedioDeExecucaoTests()
    {
        _servicos.Setup(r => r.ObterPorIds(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => _catalogo.Where(s => ids.Contains(s.Id)).ToList());
    }

    private ConsultarTempoMedioDeExecucao Caso() => new(_ordens.Object, _servicos.Object);

    private Servico NoCatalogo(string nome)
    {
        var servico = Servico.Cadastrar(nome, null, Dinheiro.De(100m));
        _catalogo.Add(servico);
        return servico;
    }

    private void OsExecutadas(params TempoDeExecucaoDaOs[] tempos) =>
        _ordens.Setup(r => r.ObterTemposDeExecucao(It.IsAny<CancellationToken>())).ReturnsAsync(tempos);

    private static TempoDeExecucaoDaOs Os(TimeSpan tempo, params Servico[] servicos) =>
        new(servicos.Select(s => s.Id).ToList(), tempo);

    [Fact]
    public async Task Troca_de_oleo_sozinha_nao_recebe_o_tempo_da_troca_de_pneu()
    {
        var oleo = NoCatalogo("Troca de oleo");
        var pneu = NoCatalogo("Troca de pneu");
        OsExecutadas(
            Os(TimeSpan.FromMinutes(55), pneu, oleo),
            Os(TimeSpan.FromMinutes(15), oleo));

        var resultado = await Caso().Executar(CancellationToken.None);

        Assert.Equal(2, resultado.QuantidadeDeOs);
        Assert.Equal(2100, resultado.TempoMedioEmSegundos);
        Assert.Equal(2, resultado.Combinacoes.Count);
        var soOleo = Assert.Single(resultado.Combinacoes, c => c.Servicos.Count == 1);
        Assert.Equal("Troca de oleo", Assert.Single(soOleo.Servicos).Nome);
        Assert.Equal(1, soOleo.QuantidadeDeOs);
        Assert.Equal(900, soOleo.TempoMedioEmSegundos);
        var oleoEPneu = Assert.Single(resultado.Combinacoes, c => c.Servicos.Count == 2);
        Assert.Equal(["Troca de oleo", "Troca de pneu"], oleoEPneu.Servicos.Select(s => s.Nome));
        Assert.Equal(3300, oleoEPneu.TempoMedioEmSegundos);
    }

    [Fact]
    public async Task Combinacoes_mais_frequentes_vem_primeiro_e_no_empate_a_de_menor_tempo_medio()
    {
        var oleo = NoCatalogo("Troca de oleo");
        var pneu = NoCatalogo("Troca de pneu");
        var alinhamento = NoCatalogo("Alinhamento");
        OsExecutadas(
            Os(TimeSpan.FromMinutes(40), alinhamento),
            Os(TimeSpan.FromMinutes(30), pneu),
            Os(TimeSpan.FromMinutes(15), oleo),
            Os(TimeSpan.FromMinutes(25), oleo));

        var resultado = await Caso().Executar(CancellationToken.None);

        Assert.Equal(
            ["Troca de oleo", "Troca de pneu", "Alinhamento"],
            resultado.Combinacoes.Select(c => Assert.Single(c.Servicos).Nome));
        Assert.Equal([2, 1, 1], resultado.Combinacoes.Select(c => c.QuantidadeDeOs));
        Assert.Equal(1200, resultado.Combinacoes[0].TempoMedioEmSegundos);
    }

    [Fact]
    public async Task Os_com_os_mesmos_servicos_em_ordem_diferente_formam_uma_unica_combinacao()
    {
        var oleo = NoCatalogo("Troca de oleo");
        var pneu = NoCatalogo("Troca de pneu");
        OsExecutadas(
            Os(TimeSpan.FromMinutes(50), oleo, pneu),
            Os(TimeSpan.FromMinutes(60), pneu, oleo));

        var resultado = await Caso().Executar(CancellationToken.None);

        var combinacao = Assert.Single(resultado.Combinacoes);
        Assert.Equal(2, combinacao.QuantidadeDeOs);
        Assert.Equal(3300, combinacao.TempoMedioEmSegundos);
    }

    [Fact]
    public async Task Servico_inativo_continua_na_combinacao_identificado_como_inativo()
    {
        var oleo = NoCatalogo("Troca de oleo");
        var lavagem = NoCatalogo("Lavagem");
        lavagem.Inativar();
        OsExecutadas(Os(TimeSpan.FromMinutes(30), oleo, lavagem));

        var resultado = await Caso().Executar(CancellationToken.None);

        var servicos = Assert.Single(resultado.Combinacoes).Servicos;
        Assert.Equal([new ServicoDaCombinacaoDto(lavagem.Id, "Lavagem", false), new ServicoDaCombinacaoDto(oleo.Id, "Troca de oleo", true)], servicos);
    }

    [Fact]
    public async Task Sem_os_elegivel_responde_vazio_sem_erro()
    {
        OsExecutadas();

        var resultado = await Caso().Executar(CancellationToken.None);

        Assert.Equal(0, resultado.QuantidadeDeOs);
        Assert.Null(resultado.TempoMedioEmSegundos);
        Assert.Empty(resultado.Combinacoes);
    }
}
