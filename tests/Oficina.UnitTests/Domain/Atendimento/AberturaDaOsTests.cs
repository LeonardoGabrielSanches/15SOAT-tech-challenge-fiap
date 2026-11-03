using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;

namespace Oficina.UnitTests.Domain.Atendimento;

public class AberturaDaOsTests
{
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
    private readonly Servico _servico = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));
    private readonly Cliente _cliente;
    private readonly Veiculo _veiculo;

    public AberturaDaOsTests()
    {
        _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
        _veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), _cliente);
    }

    private static PecaInsumo Peca(string nome, TipoPecaInsumo tipo = TipoPecaInsumo.Peca, decimal saldo = 10m)
    {
        var item = PecaInsumo.Criar(nome, tipo, "un", Dinheiro.De(25m));
        item.RegistrarEntrada(Quantidade.Criar(saldo, tipo));
        return item;
    }

    private static InclusaoDePecaInsumo Inclusao(PecaInsumo peca, decimal quantidade) =>
        new(peca, Quantidade.Criar(quantidade, peca.Tipo));

    private OrdemDeServico Abrir(params InclusaoDePecaInsumo[] pecasInsumos) =>
        OrdemDeServico.Abrir(NumeroOs.Criar(2026, 1), _cliente, _veiculo, [_servico], pecasInsumos, "atendente-1", _relogio.GetUtcNow());

    [Fact]
    public void Abrir_com_pecas_e_insumos_da_baixa_e_inclui_os_itens_na_os_recebida()
    {
        var filtro = Peca("Filtro");
        var oleo = Peca("Oleo", TipoPecaInsumo.Insumo, 20m);

        var os = Abrir(Inclusao(filtro, 1m), Inclusao(oleo, 3.5m));

        Assert.Equal(StatusOs.Recebida, os.Status);
        Assert.Equal([(filtro.Id, 1m), (oleo.Id, 3.5m)], os.ItensDePecaInsumo.Select(i => (i.PecaInsumoId, i.Quantidade)));
        Assert.Equal((9m, 16.5m), (filtro.Saldo, oleo.Saldo));
        Assert.Contains(os.EventosDeDominio, e => e is ItemIncluido i && i.PecaInsumoId == filtro.Id && i.Quantidade == 1m);
    }

    [Fact]
    public void Abrir_com_a_mesma_peca_repetida_e_rejeitado_sem_baixa()
    {
        var filtro = Peca("Filtro");

        Assert.Throws<DadosInvalidosException>(() => Abrir(Inclusao(filtro, 1m), Inclusao(filtro, 2m)));

        Assert.Equal(10m, filtro.Saldo);
    }

    [Fact]
    public void Abrir_com_peca_sem_saldo_suficiente_e_rejeitado()
    {
        var filtro = Peca("Filtro", saldo: 1m);

        Assert.Throws<DadosInvalidosException>(() => Abrir(Inclusao(filtro, 2m)));
    }

    [Fact]
    public void Abrir_com_peca_inativa_e_rejeitado()
    {
        var filtro = Peca("Filtro");
        filtro.Inativar();

        Assert.Throws<ConflitoDeEstadoException>(() => Abrir(Inclusao(filtro, 1m)));
    }

    [Fact]
    public void Depois_da_abertura_incluir_e_remover_peca_continuam_restritos_a_em_diagnostico()
    {
        var filtro = Peca("Filtro");
        var os = Abrir(Inclusao(filtro, 1m));

        Assert.Throws<ConflitoDeEstadoException>(() => os.IncluirPecaInsumo(filtro, Quantidade.Criar(1m, filtro.Tipo), _relogio.GetUtcNow()));
        Assert.Throws<ConflitoDeEstadoException>(() => os.RemoverPecaInsumo(filtro, null));
        Assert.Equal(9m, filtro.Saldo);
    }
}
