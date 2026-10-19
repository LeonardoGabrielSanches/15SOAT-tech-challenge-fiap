using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Application.Atendimento.UseCases;

public sealed class ListarFilaDeExecucao(IOrdemDeServicoRepository ordens)
{
    public async Task<IReadOnlyList<OrdemDeServicoResumoDto>> Executar(CancellationToken cancellationToken)
    {
        var aprovadas = await ordens.Listar(StatusOs.Aprovada, cancellationToken);

        return aprovadas
            .OrderBy(o => o.AbertaEm)
            .Select(OrdemDeServicoResumoDto.De)
            .ToList();
    }
}

public sealed class IniciarExecucao(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(Guid osId, string autor, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);

        os.IniciarExecucao(autor, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed class FinalizarExecucao(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(Guid osId, string autor, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);

        os.FinalizarExecucao(autor, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed class RegistrarEntrega(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(Guid osId, string autor, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);

        os.RegistrarEntrega(autor, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed record ServicoDaCombinacaoDto(Guid Id, string Nome, bool Ativo)
{
    public static ServicoDaCombinacaoDto De(Servico servico) => new(servico.Id, servico.Nome, servico.Ativo);
}

public sealed record TempoMedioDaCombinacaoDto(IReadOnlyList<ServicoDaCombinacaoDto> Servicos, int QuantidadeDeOs, double TempoMedioEmSegundos);

public sealed record TempoMedioDeExecucaoDto(int QuantidadeDeOs, double? TempoMedioEmSegundos, IReadOnlyList<TempoMedioDaCombinacaoDto> Combinacoes);

public sealed class ConsultarTempoMedioDeExecucao(IOrdemDeServicoRepository ordens, IServicoRepository servicos)
{
    public async Task<TempoMedioDeExecucaoDto> Executar(CancellationToken cancellationToken)
    {
        var tempos = await ordens.ObterTemposDeExecucao(cancellationToken);

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
