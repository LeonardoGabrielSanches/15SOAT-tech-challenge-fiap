using Moq;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;
using Oficina.UnitTests.Domain.Atendimento;

namespace Oficina.UnitTests.Application.Atendimento;

public class ConsultaPublicaDaOsTests
{
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly TimeProvider _relogio = RelogioFixo.Em(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
    private readonly Cliente _cliente = Cliente.Cadastrar("Maria Silva", Documento.Criar("529.982.247-25"), Email.Criar("maria@oficina.com"), null);
    private readonly OrdemDeServico _os;

    public ConsultaPublicaDaOsTests()
    {
        var veiculo = Veiculo.Cadastrar(Placa.Criar("ABC1D23"), "Fiat", "Uno", AnoFabricacao.Criar(2015, _relogio), _cliente);
        var servico = Servico.Cadastrar("Troca de oleo", null, Dinheiro.De(100m));
        _os = OrdemDeServico.Abrir(NumeroOs.Criar(2026, 7), _cliente, veiculo, [servico], "joao.atendente", _relogio.GetUtcNow());
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
}
