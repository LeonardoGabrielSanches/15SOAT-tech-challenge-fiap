using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Interfaces;

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
