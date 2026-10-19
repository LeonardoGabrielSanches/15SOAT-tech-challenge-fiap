using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Interfaces;
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

public sealed record ServicoDaCombinacaoDto(Guid Id, string Nome, bool Ativo);

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

        var catalogo = (await servicos.ObterPorIds(tempos.SelectMany(t => t.ServicoIds).Distinct().ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        var combinacoes = tempos
            .GroupBy(t => string.Join(',', t.ServicoIds.Order()))
            .Select(g => new TempoMedioDaCombinacaoDto(
                g.First().ServicoIds
                    .Select(id => catalogo[id])
                    .Select(s => new ServicoDaCombinacaoDto(s.Id, s.Nome, s.Ativo))
                    .OrderBy(s => s.Nome)
                    .ToList(),
                g.Count(),
                g.Average(t => t.Tempo.TotalSeconds)))
            .OrderByDescending(c => c.QuantidadeDeOs)
            .ThenBy(c => c.TempoMedioEmSegundos)
            .ToList();

        return new TempoMedioDeExecucaoDto(tempos.Count, tempos.Average(t => t.Tempo.TotalSeconds), combinacoes);
    }
}
