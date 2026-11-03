using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.OrdensDeServico;

internal static class PermissaoDeServicosDaOs
{
    public static void Garantir(OrdemDeServico os, PapelDoUsuario papel)
    {
        if (papel.HasFlag(PapelDoUsuario.Gerente))
        {
            return;
        }

        var exigido = os.Status switch
        {
            StatusOs.Recebida => PapelDoUsuario.Atendente,
            StatusOs.EmDiagnostico => PapelDoUsuario.Mecanico,
            _ => PapelDoUsuario.Nenhum
        };

        if (exigido != PapelDoUsuario.Nenhum && (papel & exigido) == 0)
        {
            throw new AcessoNegadoException($"Em uma OS {os.Status} os servicos so podem ser ajustados pelo {exigido.ToString().ToLowerInvariant()}.");
        }
    }
}
