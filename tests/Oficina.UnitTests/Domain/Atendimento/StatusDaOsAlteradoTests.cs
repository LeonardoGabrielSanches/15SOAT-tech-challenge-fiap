using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.UnitTests.Domain.Atendimento;

public class StatusDaOsAlteradoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly Servico _servico = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));
    private readonly Cliente _cliente;
    private readonly Veiculo _veiculo;

    public StatusDaOsAlteradoTests()
    {
        _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
        _veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, RelogioFixo.Em(Agora)), _cliente);
    }

    private OrdemDeServico Abrir() =>
        OrdemDeServico.Abrir(NumeroOs.Criar(2026, 1), _cliente, _veiculo, [_servico], [], "atendente-1", Agora);

    [Fact]
    public void Abertura_registra_a_mudanca_de_status_com_origem_vazia()
    {
        var os = Abrir();

        var evento = Assert.Single(os.EventosDeDominio.OfType<StatusDaOsAlterado>());
        Assert.Equal(new StatusDaOsAlterado(os.Id, "OS-2026-00001", _cliente.Id, null, StatusOs.Recebida, Agora), evento);
        Assert.Contains(os.EventosDeDominio, e => e is OsAberta);
        var entrada = Assert.Single(os.Historico);
        Assert.Equal(((StatusOs?)null, StatusOs.Recebida, "atendente-1"), (entrada.Origem, entrada.Destino, entrada.Autor));
    }

    [Fact]
    public void Cada_transicao_do_fluxo_registra_uma_mudanca_de_status_com_origem_e_destino()
    {
        var os = Abrir();
        os.IniciarDiagnostico("mecanico-1", Agora);
        os.ConcluirDiagnostico([_servico], [], "mecanico-1", Agora);
        os.RecusarOrcamento(os.VersaoVigente!.Token, "Caro", "Cliente", Agora);
        os.ConcluirDiagnostico([_servico], [], "mecanico-1", Agora);
        os.AprovarOrcamento(os.VersaoVigente!.Token, "Cliente", Agora);
        os.IniciarExecucao("mecanico-1", Agora);
        os.FinalizarExecucao("mecanico-1", Agora);
        os.RegistrarEntrega("atendente-1", Agora);

        var transicoes = os.EventosDeDominio.OfType<StatusDaOsAlterado>().Select(e => (e.Origem, e.Destino));

        Assert.Equal(
            [
                (null, StatusOs.Recebida),
                (StatusOs.Recebida, StatusOs.EmDiagnostico),
                (StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao),
                (StatusOs.AguardandoAprovacao, StatusOs.EmDiagnostico),
                (StatusOs.EmDiagnostico, StatusOs.AguardandoAprovacao),
                (StatusOs.AguardandoAprovacao, StatusOs.Aprovada),
                (StatusOs.Aprovada, StatusOs.EmExecucao),
                (StatusOs.EmExecucao, StatusOs.Finalizada),
                (StatusOs.Finalizada, StatusOs.Entregue)
            ],
            transicoes);
        Assert.All(os.EventosDeDominio.OfType<StatusDaOsAlterado>(), e =>
            Assert.Equal((os.Id, "OS-2026-00001", _cliente.Id, Agora), (e.OsId, e.Numero, e.ClienteId, e.OcorridoEm)));
    }

    [Fact]
    public void Cada_transicao_registra_o_mesmo_par_de_origem_e_destino_do_historico()
    {
        var os = Abrir();
        os.IniciarDiagnostico("mecanico-1", Agora);
        os.ConcluirDiagnostico([_servico], [], "mecanico-1", Agora);

        var transicoes = os.EventosDeDominio.OfType<StatusDaOsAlterado>().Select(e => (e.Origem, e.Destino, e.OcorridoEm));

        Assert.Equal(os.Historico.Select(h => (h.Origem, h.Destino, h.Momento)), transicoes);
    }

    [Theory]
    [InlineData(StatusOs.Recebida)]
    [InlineData(StatusOs.EmDiagnostico)]
    [InlineData(StatusOs.AguardandoAprovacao)]
    public void Cancelamento_registra_a_mudanca_para_cancelada(StatusOs status)
    {
        var os = Abrir();
        if (status != StatusOs.Recebida) os.IniciarDiagnostico("mecanico-1", Agora);
        if (status == StatusOs.AguardandoAprovacao) os.ConcluirDiagnostico([_servico], [], "mecanico-1", Agora);

        os.Cancelar("Desistiu", [], "gerente-1", Agora);

        var ultimo = os.EventosDeDominio.OfType<StatusDaOsAlterado>().Last();
        Assert.Equal((status, StatusOs.Cancelada), (ultimo.Origem!.Value, ultimo.Destino));
        Assert.Contains(os.EventosDeDominio, e => e is OsCancelada);
    }

    [Fact]
    public void Transicao_recusada_nao_registra_mudanca_de_status()
    {
        var os = Abrir();

        Assert.Throws<ConflitoDeEstadoException>(() => os.IniciarExecucao("mecanico-1", Agora));

        Assert.Single(os.EventosDeDominio.OfType<StatusDaOsAlterado>());
    }
}
