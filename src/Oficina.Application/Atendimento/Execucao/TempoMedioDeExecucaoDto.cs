using Oficina.Domain.CatalogoEstoque.Entities;

namespace Oficina.Application.Atendimento.Execucao;

public sealed record ServicoDaCombinacaoDto(Guid Id, string Nome, bool Ativo)
{
    public static ServicoDaCombinacaoDto De(Servico servico) => new(servico.Id, servico.Nome, servico.Ativo);
}

public sealed record TempoMedioDaCombinacaoDto(IReadOnlyList<ServicoDaCombinacaoDto> Servicos, int QuantidadeDeOs, double TempoMedioEmSegundos);

public sealed record TempoMedioDeExecucaoDto(int QuantidadeDeOs, double? TempoMedioEmSegundos, IReadOnlyList<TempoMedioDaCombinacaoDto> Combinacoes);
