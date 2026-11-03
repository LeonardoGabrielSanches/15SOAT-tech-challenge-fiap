using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;

namespace Oficina.Infrastructure.Persistencia.Atendimento;

public sealed class ConsultaDeOrdensDeServico(DbContext contexto) : IConsultaDeOrdensDeServico
{
    private static readonly StatusOs[] StatusDaListagemPadrao =
        Enum.GetValues<StatusOs>().Where(s => s.ApareceNaListagemPadrao()).ToArray();

    private static readonly Expression<Func<OrdemDeServico, int>> PrioridadeNaListagem = MontarPrioridadeNaListagem();

    public async Task<PaginaDeOrdensDeServicoDto> Listar(StatusOs? status, int pagina, int tamanho, CancellationToken cancellationToken)
    {
        var filtradas = Filtradas(status);
        var total = await filtradas.CountAsync(cancellationToken);
        var deslocamento = (long)(pagina - 1) * tamanho;

        IReadOnlyList<OrdemDeServicoResumoDto> itens = deslocamento >= total
            ? []
            : await Resumos(Ordenadas(filtradas).Skip((int)deslocamento).Take(tamanho), cancellationToken);

        return new PaginaDeOrdensDeServicoDto(itens, pagina, tamanho, total);
    }

    public Task<IReadOnlyList<OrdemDeServicoResumoDto>> ListarPorStatus(StatusOs status, CancellationToken cancellationToken) =>
        Resumos(Ordenadas(Filtradas(status)), cancellationToken);

    public async Task<IReadOnlyList<TempoDeExecucaoDaOs>> ObterTemposDeExecucao(CancellationToken cancellationToken)
    {
        var intervalos = await contexto.Set<OrdemDeServico>()
            .Where(o => o.Historico.Any(h => h.Destino == StatusOs.Finalizada))
            .Select(o => new
            {
                ServicoIds = o.Servicos.Select(s => s.ServicoId).ToList(),
                Inicio = o.Historico.Where(h => h.Destino == StatusOs.EmExecucao).Max(h => h.Momento),
                Fim = o.Historico.Where(h => h.Destino == StatusOs.Finalizada).Max(h => h.Momento)
            })
            .ToListAsync(cancellationToken);

        return intervalos.Select(i => new TempoDeExecucaoDaOs(CombinacaoDeServicos.De(i.ServicoIds), i.Fim - i.Inicio)).ToList();
    }

    private IQueryable<OrdemDeServico> Filtradas(StatusOs? status)
    {
        var ordens = contexto.Set<OrdemDeServico>().AsNoTracking();

        return status is null
            ? ordens.Where(o => StatusDaListagemPadrao.Contains(o.Status))
            : ordens.Where(o => o.Status == status);
    }

    private static IQueryable<OrdemDeServico> Ordenadas(IQueryable<OrdemDeServico> ordens) =>
        ordens.OrderBy(PrioridadeNaListagem).ThenBy(o => o.AbertaEm).ThenBy(o => o.Id);

    private static async Task<IReadOnlyList<OrdemDeServicoResumoDto>> Resumos(
        IQueryable<OrdemDeServico> ordens,
        CancellationToken cancellationToken)
    {
        var linhas = await ordens
            .Select(o => new { o.Id, o.Numero, o.Status, o.ClienteId, o.VeiculoId, o.AbertaEm })
            .ToListAsync(cancellationToken);

        return linhas
            .Select(l => new OrdemDeServicoResumoDto(l.Id, l.Numero.Valor, l.Status.ToString(), l.ClienteId, l.VeiculoId, l.AbertaEm))
            .ToList();
    }

    private static Expression<Func<OrdemDeServico, int>> MontarPrioridadeNaListagem()
    {
        var os = Expression.Parameter(typeof(OrdemDeServico), "o");
        var status = Expression.Property(os, nameof(OrdemDeServico.Status));

        var prioridade = Enum.GetValues<StatusOs>().Aggregate(
            (Expression)Expression.Constant(int.MaxValue),
            (senao, valor) => Expression.Condition(
                Expression.Equal(status, Expression.Constant(valor)),
                Expression.Constant(valor.PrioridadeNaListagem()),
                senao));

        return Expression.Lambda<Func<OrdemDeServico, int>>(prioridade, os);
    }
}
