using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed class CancelarOs(
    IOrdemDeServicoRepository ordens,
    IPecaInsumoRepository pecas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task Executar(Guid osId, string motivo, string autor, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        var pecasDaOs = await PecasInsumosDaOs.Carregar(pecas, os, cancellationToken);

        os.Cancelar(motivo, pecasDaOs, autor, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
