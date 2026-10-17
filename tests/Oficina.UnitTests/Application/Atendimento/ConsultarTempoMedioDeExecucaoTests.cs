using Moq;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.UnitTests.Application.Atendimento;

public class ConsultarTempoMedioDeExecucaoTests
{
    private readonly Mock<IOrdemDeServicoRepository> _ordens = new();

    private ConsultarTempoMedioDeExecucao Caso() => new(_ordens.Object);

    [Fact]
    public async Task Calcula_a_media_dos_tempos_das_os_elegiveis()
    {
        _ordens.Setup(r => r.ObterTemposDeExecucao(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([TimeSpan.FromHours(1), TimeSpan.FromHours(3)]);

        var resultado = await Caso().Executar(null, CancellationToken.None);

        Assert.Equal(2, resultado.QuantidadeDeOs);
        Assert.Equal(TimeSpan.FromHours(2).TotalSeconds, resultado.TempoMedioEmSegundos);
    }

    [Fact]
    public async Task Repassa_o_filtro_de_servico_ao_repositorio()
    {
        var servicoId = Guid.NewGuid();
        _ordens.Setup(r => r.ObterTemposDeExecucao(servicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([TimeSpan.FromMinutes(90)]);

        var resultado = await Caso().Executar(servicoId, CancellationToken.None);

        Assert.Equal(1, resultado.QuantidadeDeOs);
        Assert.Equal(5400, resultado.TempoMedioEmSegundos);
        _ordens.Verify(r => r.ObterTemposDeExecucao(servicoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sem_os_elegivel_responde_vazio_sem_erro()
    {
        _ordens.Setup(r => r.ObterTemposDeExecucao(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await Caso().Executar(null, CancellationToken.None);

        Assert.Equal(0, resultado.QuantidadeDeOs);
        Assert.Null(resultado.TempoMedioEmSegundos);
    }
}
