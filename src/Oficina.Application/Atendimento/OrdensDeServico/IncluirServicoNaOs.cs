using Oficina.Application.Catalogo;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Interfaces;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed class IncluirServicoNaOs(IOrdemDeServicoRepository ordens, IServicoRepository servicos, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid osId, Guid servicoId, PapelDoUsuario papel, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        PermissaoDeServicosDaOs.Garantir(os, papel);
        var servico = await BuscaDeServico.ObterOuFalhar(servicos, servicoId, cancellationToken);

        os.IncluirServico(servico);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
