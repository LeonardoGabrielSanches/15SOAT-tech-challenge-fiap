using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Orcamento;

public sealed class AprovarOrcamento(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(string? token, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOsPorToken.ObterOuFalhar(ordens, token, cancellationToken);

        os.AprovarOrcamento(token!, AutorDoHistorico.Cliente, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
