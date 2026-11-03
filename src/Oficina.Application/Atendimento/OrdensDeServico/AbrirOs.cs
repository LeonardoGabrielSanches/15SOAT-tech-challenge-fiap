using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed record PecaInsumoNaAbertura(Guid PecaInsumoId, decimal Quantidade);

public sealed record AbrirOsComando(
    Guid ClienteId,
    Guid VeiculoId,
    IReadOnlyCollection<Guid> ServicoIds,
    IReadOnlyCollection<PecaInsumoNaAbertura> PecasInsumos,
    string Autor);

public sealed class AbrirOs(
    IOrdemDeServicoRepository ordens,
    IClienteRepository clientes,
    IVeiculoRepository veiculos,
    IServicoRepository servicos,
    IPecaInsumoRepository pecas,
    IGeradorNumeroOs gerador,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<OrdemDeServicoResumoDto> Executar(AbrirOsComando comando, CancellationToken cancellationToken)
    {
        var cliente = await BuscaDeCliente.ObterOuFalhar(clientes, comando.ClienteId, cancellationToken);
        var veiculo = await BuscaDeVeiculo.ObterOuFalhar(veiculos, comando.VeiculoId, cancellationToken);
        var servicosDaOs = await ObterServicos(comando.ServicoIds, cancellationToken);
        var inclusoes = await ObterInclusoesDePecasInsumos(comando.PecasInsumos, cancellationToken);

        if (await ordens.ExisteEmAndamentoDoVeiculo(veiculo.Id, cancellationToken))
        {
            throw new ConflitoDeEstadoException("O Veiculo ja possui uma OS em andamento.");
        }

        var agora = relogio.GetUtcNow();
        await using var transacao = await unidadeDeTrabalho.IniciarTransacao(cancellationToken);
        var numero = await gerador.Proximo(agora.Year, cancellationToken);
        var os = OrdemDeServico.Abrir(numero, cliente, veiculo, servicosDaOs, inclusoes, comando.Autor, agora);

        ordens.Adicionar(os);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return OrdemDeServicoResumoDto.De(os);
    }

    private async Task<IReadOnlyList<Servico>> ObterServicos(IReadOnlyCollection<Guid> servicoIds, CancellationToken cancellationToken)
    {
        var encontrados = (await servicos.ObterPorIds(servicoIds.Distinct().ToList(), cancellationToken)).ToDictionary(s => s.Id);

        return servicoIds
            .Select(id => encontrados.TryGetValue(id, out var servico)
                ? servico
                : throw new RecursoNaoEncontradoException("Servico nao encontrado."))
            .ToList();
    }

    private async Task<IReadOnlyList<InclusaoDePecaInsumo>> ObterInclusoesDePecasInsumos(
        IReadOnlyCollection<PecaInsumoNaAbertura> pecasInsumos,
        CancellationToken cancellationToken)
    {
        var inclusoes = new List<InclusaoDePecaInsumo>();
        foreach (var pecaInsumo in pecasInsumos)
        {
            var peca = await PecasInsumosDaOs.ObterOuFalhar(pecas, pecaInsumo.PecaInsumoId, cancellationToken);
            inclusoes.Add(new InclusaoDePecaInsumo(peca, Quantidade.Criar(pecaInsumo.Quantidade, peca.Tipo)));
        }

        return inclusoes;
    }
}
