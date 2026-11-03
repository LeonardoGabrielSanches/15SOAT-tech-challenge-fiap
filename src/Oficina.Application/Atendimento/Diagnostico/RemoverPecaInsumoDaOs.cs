using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Atendimento.Diagnostico;

public sealed class RemoverPecaInsumoDaOs(
    IOrdemDeServicoRepository ordens,
    IPecaInsumoRepository pecas,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid osId, Guid pecaInsumoId, decimal? quantidade, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        var peca = await PecasInsumosDaOs.ObterOuFalhar(pecas, pecaInsumoId, cancellationToken);

        if (quantidade is not null)
        {
            Quantidade.Criar(quantidade.Value, peca.Tipo);
        }

        os.RemoverPecaInsumo(peca, quantidade);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
