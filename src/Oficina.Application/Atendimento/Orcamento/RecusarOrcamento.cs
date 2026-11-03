using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Orcamento;

public sealed class RecusarOrcamento(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task Executar(string? token, string? motivo, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOsPorToken.ObterOuFalhar(ordens, token, cancellationToken);

        os.RecusarOrcamento(token!, motivo, AutorDoHistorico.Cliente, relogio.GetUtcNow());

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
