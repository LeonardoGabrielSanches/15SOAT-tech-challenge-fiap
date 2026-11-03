using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Execucao;

public sealed class RegistrarEntrega(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(Guid osId, string autor, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);

        os.RegistrarEntrega(autor, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
