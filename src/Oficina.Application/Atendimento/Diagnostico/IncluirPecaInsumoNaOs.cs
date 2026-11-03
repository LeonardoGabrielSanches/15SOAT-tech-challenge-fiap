using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Atendimento.Diagnostico;

public sealed class IncluirPecaInsumoNaOs(
    IOrdemDeServicoRepository ordens,
    IPecaInsumoRepository pecas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task Executar(Guid osId, Guid pecaInsumoId, decimal quantidade, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        var peca = await PecasInsumosDaOs.ObterOuFalhar(pecas, pecaInsumoId, cancellationToken);

        os.IncluirPecaInsumo(peca, Quantidade.Criar(quantidade, peca.Tipo), relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
