using Moq;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.UnitTests.Domain.Atendimento;

namespace Oficina.UnitTests.Application.Atendimento;

public class ConsultaPublicaDaOsTests
{
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
    private readonly Cliente _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
    private readonly OrdemDeServico _os;
    private readonly Servico _servicoInicial = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));

    public ConsultaPublicaDaOsTests()
    {
        var veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), _cliente);
        _os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 7), _cliente, veiculo, [_servicoInicial], "joao.atendente", _relogio.GetUtcNow());
        _ordens.Setup(r => r.ObterPorNumero("OS-2026-00007", It.IsAny<CancellationToken>())).ReturnsAsync(_os);
        _clientes.Setup(r => r.ObterPorId(_cliente.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_cliente);
    }

    private ConsultarOsPublica Caso() => new(_ordens.Object, _clientes.Object);

    [Fact]
    public async Task Retorna_status_e_historico_com_autor_generico()
    {
        var consulta = await Caso().Executar("OS-2026-00007", "52998224725", CancellationToken.None);

        Assert.Equal("OS-2026-00007", consulta.Numero);
        Assert.Equal("Recebida", consulta.Status);
        var entrada = Assert.Single(consulta.Historico);
        Assert.Equal("Oficina", entrada.Autor);
        Assert.Null(consulta.OrcamentoVigente);
    }

    [Theory]
    [InlineData("OS-2026-00007", "111.444.777-35")]
    [InlineData("OS-2026-00007", "abc")]
    [InlineData("OS-2026-99999", "52998224725")]
    [InlineData("", "52998224725")]
    public async Task Documento_divergente_ou_os_inexistente_geram_a_mesma_excecao(string numero, string documento)
    {
        var excecao = await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => Caso().Executar(numero, documento, CancellationToken.None));

        Assert.Equal("OS nao encontrada.", excecao.Message);
    }

    [Fact]
    public async Task Orcamento_vigente_e_a_ultima_versao_com_itens_congelados()
    {
        var servico = Servico.Cadastrar("Alinhamento", null, Dinheiro.De(80m));
        var peca = PecaInsumo.Criar("Filtro", TipoPecaInsumo.Peca, "un", Dinheiro.De(25m));
        peca.RegistrarEntrada(Quantidade.Criar(5m, TipoPecaInsumo.Peca));
        _os.IncluirServico(servico);
        _os.IniciarDiagnostico("m", _relogio.GetUtcNow());
        _os.IncluirPecaInsumo(peca, Quantidade.Criar(2m, TipoPecaInsumo.Peca), _relogio.GetUtcNow());
        _os.ConcluirDiagnostico([_servicoInicial, servico], [peca], "m", _relogio.GetUtcNow());

        var consulta = await Caso().Executar("OS-2026-00007", "52998224725", CancellationToken.None);

        var orcamento = consulta.OrcamentoVigente!;
        Assert.Equal((1, "Pendente"), (orcamento.Versao, orcamento.Situacao));
        Assert.Equal(3, orcamento.Itens.Count);
        Assert.Contains(orcamento.Itens, i => i is { Descricao: "Filtro", Quantidade: 2m, PrecoUnitario: 25m, Subtotal: 50m });
        Assert.Equal(orcamento.Itens.Sum(i => i.Subtotal), orcamento.Total);
        Assert.DoesNotContain("Token", typeof(Oficina.Application.Atendimento.UseCases.OrcamentoPublicoDto).GetProperties().Select(p => p.Name));
    }
}
