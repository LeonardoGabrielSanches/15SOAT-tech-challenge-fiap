using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Application.Atendimento.Execucao;

public sealed class ConsultarTempoMedioDeExecucao(IConsultaDeOrdensDeServico consulta, IServicoRepository servicos)
{
    public async Task<TempoMedioDeExecucaoDto> Executar(CancellationToken cancellationToken)
    {
        var tempos = await consulta.ObterTemposDeExecucao(cancellationToken);

        if (tempos.Count == 0)
        {
            return new TempoMedioDeExecucaoDto(0, null, []);
        }

        var catalogo = await CatalogoDosServicos(tempos, cancellationToken);

        var combinacoes = tempos
            .GroupBy(t => t.Combinacao)
            .Select(osDaCombinacao => TempoMedioDaCombinacao(osDaCombinacao.Key, osDaCombinacao.ToList(), catalogo));

        return new TempoMedioDeExecucaoDto(tempos.Count, MediaEmSegundos(tempos), DaMaisFrequenteParaAMenosFrequente(combinacoes));
    }

    private async Task<IReadOnlyDictionary<Guid, Servico>> CatalogoDosServicos(
        IReadOnlyList<TempoDeExecucaoDaOs> tempos,
        CancellationToken cancellationToken)
    {
        var servicoIds = tempos.SelectMany(t => t.Combinacao.ServicoIds).Distinct().ToList();

        return (await servicos.ObterPorIds(servicoIds, cancellationToken)).ToDictionary(s => s.Id);
    }

    private static TempoMedioDaCombinacaoDto TempoMedioDaCombinacao(
        CombinacaoDeServicos combinacao,
        IReadOnlyList<TempoDeExecucaoDaOs> tempos,
        IReadOnlyDictionary<Guid, Servico> catalogo) =>
        new(
            combinacao.ServicoIds
                .Select(id => ServicoDoCatalogo(id, catalogo))
                .Select(ServicoDaCombinacaoDto.De)
                .OrderBy(s => s.Nome)
                .ToList(),
            tempos.Count,
            MediaEmSegundos(tempos));

    private static Servico ServicoDoCatalogo(Guid servicoId, IReadOnlyDictionary<Guid, Servico> catalogo) =>
        catalogo.TryGetValue(servicoId, out var servico)
            ? servico
            : throw new InvalidOperationException(
                $"Servico {servicoId} de uma OS finalizada nao esta no catalogo, mas Servico e apenas inativado, nunca excluido.");

    private static List<TempoMedioDaCombinacaoDto> DaMaisFrequenteParaAMenosFrequente(IEnumerable<TempoMedioDaCombinacaoDto> combinacoes) =>
        combinacoes
            .OrderByDescending(c => c.QuantidadeDeOs)
            .ThenBy(c => c.TempoMedioEmSegundos)
            .ThenBy(c => string.Join(" + ", c.Servicos.Select(s => s.Nome)))
            .ThenBy(c => string.Join(',', c.Servicos.Select(s => s.Id)), StringComparer.Ordinal)
            .ToList();

    private static double MediaEmSegundos(IEnumerable<TempoDeExecucaoDaOs> tempos) =>
        tempos.Average(t => t.Tempo.TotalSeconds);
}
