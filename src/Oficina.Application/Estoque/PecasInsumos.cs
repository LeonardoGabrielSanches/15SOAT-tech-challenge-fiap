using Oficina.Application.Common;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque;

namespace Oficina.Application.Estoque;

public sealed record PecaInsumoDto(
    Guid Id,
    string Nome,
    TipoPecaInsumo Tipo,
    string UnidadeDeMedida,
    decimal Preco,
    decimal Saldo,
    bool Ativo)
{
    public static PecaInsumoDto De(PecaInsumo item) =>
        new(item.Id, item.Nome, item.Tipo, item.UnidadeDeMedida, item.Preco.Valor, item.Saldo, item.Ativo);
}

public sealed record SaldoDto(Guid Id, string Nome, string UnidadeDeMedida, decimal Saldo);

public sealed record CadastrarPecaInsumo(string Nome, TipoPecaInsumo Tipo, string UnidadeDeMedida, decimal Preco);

public sealed record AtualizarPecaInsumo(string Nome, string UnidadeDeMedida, decimal Preco);

public sealed class CadastrarPecaInsumoCasoDeUso(IRepositorioDePecaInsumo repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<PecaInsumoDto> Executar(CadastrarPecaInsumo comando, CancellationToken cancellationToken)
    {
        var item = PecaInsumo.Criar(comando.Nome, comando.Tipo, comando.UnidadeDeMedida, Dinheiro.De(comando.Preco));
        repositorio.Adicionar(item);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return PecaInsumoDto.De(item);
    }
}

public sealed class AtualizarPecaInsumoCasoDeUso(IRepositorioDePecaInsumo repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<PecaInsumoDto> Executar(Guid id, AtualizarPecaInsumo comando, CancellationToken cancellationToken)
    {
        var item = await PecasInsumosConsulta.ObterOuFalhar(repositorio, id, cancellationToken);
        item.Atualizar(comando.Nome, comando.UnidadeDeMedida, Dinheiro.De(comando.Preco));
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return PecaInsumoDto.De(item);
    }
}

public sealed class InativarPecaInsumoCasoDeUso(IRepositorioDePecaInsumo repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid id, CancellationToken cancellationToken)
    {
        var item = await PecasInsumosConsulta.ObterOuFalhar(repositorio, id, cancellationToken);
        item.Inativar();
        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed class RegistrarEntradaDeMercadoriaCasoDeUso(IRepositorioDePecaInsumo repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<SaldoDto> Executar(Guid id, decimal quantidade, CancellationToken cancellationToken)
    {
        var item = await PecasInsumosConsulta.ObterOuFalhar(repositorio, id, cancellationToken);
        item.RegistrarEntrada(Quantidade.Criar(quantidade, item.Tipo));
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return new SaldoDto(item.Id, item.Nome, item.UnidadeDeMedida, item.Saldo);
    }
}

public sealed class ConsultarPecasInsumosCasoDeUso(IRepositorioDePecaInsumo repositorio)
{
    public async Task<PecaInsumoDto> Obter(Guid id, CancellationToken cancellationToken) =>
        PecaInsumoDto.De(await PecasInsumosConsulta.ObterOuFalhar(repositorio, id, cancellationToken));

    public async Task<IReadOnlyList<PecaInsumoDto>> Listar(bool incluirInativos, CancellationToken cancellationToken) =>
        (await repositorio.Listar(incluirInativos, cancellationToken)).Select(PecaInsumoDto.De).ToList();

    public async Task<SaldoDto> ConsultarSaldo(Guid id, CancellationToken cancellationToken)
    {
        var item = await PecasInsumosConsulta.ObterOuFalhar(repositorio, id, cancellationToken);
        return new SaldoDto(item.Id, item.Nome, item.UnidadeDeMedida, item.Saldo);
    }
}

internal static class PecasInsumosConsulta
{
    public static async Task<PecaInsumo> ObterOuFalhar(
        IRepositorioDePecaInsumo repositorio, Guid id, CancellationToken cancellationToken) =>
        await repositorio.Obter(id, cancellationToken)
        ?? throw new RecursoNaoEncontradoException("Peça/insumo não encontrada.");
}
