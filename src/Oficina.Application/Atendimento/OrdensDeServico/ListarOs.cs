using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed class ListarOs(IConsultaDeOrdensDeServico consulta)
{
    public const int PaginaPadrao = 1;
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 100;

    public Task<PaginaDeOrdensDeServicoDto> Executar(StatusOs? status, int pagina, int tamanho, CancellationToken cancellationToken)
    {
        if (pagina < 1)
        {
            throw new DadosInvalidosException("A pagina deve ser no minimo 1.");
        }

        if (tamanho is < 1 or > TamanhoMaximo)
        {
            throw new DadosInvalidosException($"O tamanho da pagina deve ser de 1 a {TamanhoMaximo}.");
        }

        return consulta.Listar(status, pagina, tamanho, cancellationToken);
    }
}
