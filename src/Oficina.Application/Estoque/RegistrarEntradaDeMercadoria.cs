using Oficina.Application.Common;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Estoque;

public sealed class RegistrarEntradaDeMercadoria(IPecaInsumoRepository repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<SaldoDto> Executar(Guid id, decimal quantidade, CancellationToken cancellationToken)
    {
        var item = await BuscaDePecaInsumo.ObterOuFalhar(repositorio, id, cancellationToken);
        item.RegistrarEntrada(Quantidade.Criar(quantidade, item.Tipo));
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return SaldoDto.De(item);
    }
}
