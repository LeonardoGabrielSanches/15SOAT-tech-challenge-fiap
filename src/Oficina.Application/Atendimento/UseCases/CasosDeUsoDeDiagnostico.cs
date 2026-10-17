using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque;

namespace Oficina.Application.Atendimento.UseCases;

internal static class BuscaDePecaInsumo
{
    public static async Task<PecaInsumo> ObterOuFalhar(IRepositorioDePecaInsumo pecas, Guid id, CancellationToken cancellationToken) =>
        await pecas.Obter(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Peca/insumo nao encontrada.");
}

public sealed class IniciarDiagnostico(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(Guid osId, string autor, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);

        os.IniciarDiagnostico(autor, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed class IncluirPecaInsumoNaOs(
    IOrdemDeServicoRepository ordens,
    IRepositorioDePecaInsumo pecas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task Executar(Guid osId, Guid pecaInsumoId, decimal quantidade, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        var peca = await BuscaDePecaInsumo.ObterOuFalhar(pecas, pecaInsumoId, cancellationToken);

        os.IncluirPecaInsumo(peca, Quantidade.Criar(quantidade, peca.Tipo), relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed class RemoverPecaInsumoDaOs(
    IOrdemDeServicoRepository ordens,
    IRepositorioDePecaInsumo pecas,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid osId, Guid pecaInsumoId, decimal? quantidade, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        var peca = await BuscaDePecaInsumo.ObterOuFalhar(pecas, pecaInsumoId, cancellationToken);

        if (quantidade is not null)
        {
            Quantidade.Criar(quantidade.Value, peca.Tipo);
        }

        os.RemoverPecaInsumo(peca, quantidade);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
