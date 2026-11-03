using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Atendimento.Diagnostico;

public sealed class ConcluirDiagnostico(
    IOrdemDeServicoRepository ordens,
    IServicoRepository servicos,
    IPecaInsumoRepository pecas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task Executar(Guid osId, string autor, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        var servicosDaOs = await servicos.ObterPorIds(os.Servicos.Select(s => s.ServicoId).ToList(), cancellationToken);
        var pecasDaOs = await PecasInsumosDaOs.Carregar(pecas, os, cancellationToken);

        os.ConcluirDiagnostico(servicosDaOs, pecasDaOs, autor, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
