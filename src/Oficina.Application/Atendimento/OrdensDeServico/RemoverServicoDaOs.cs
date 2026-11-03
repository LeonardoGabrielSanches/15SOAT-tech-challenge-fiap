using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed class RemoverServicoDaOs(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid osId, Guid servicoId, PapelDoUsuario papel, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        PermissaoDeServicosDaOs.Garantir(os, papel);

        os.RemoverServico(servicoId);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
