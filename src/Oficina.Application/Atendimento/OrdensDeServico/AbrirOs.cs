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
    string Autor,
    IReadOnlyCollection<PecaInsumoNaAbertura>? PecasInsumos = null);

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
        var servicosSolicitados = await ServicosSolicitados(comando.ServicoIds, cancellationToken);
        var pecasSolicitadas = await PecasInsumosSolicitadas(comando.PecasInsumos ?? [], cancellationToken);

        OrdemDeServico.GarantirAberturaPermitida(cliente, veiculo, servicosSolicitados, pecasSolicitadas);

        if (await ordens.ExisteEmAndamentoDoVeiculo(veiculo.Id, cancellationToken))
        {
            throw new ConflitoDeEstadoException("O Veiculo ja possui uma OS em andamento.");
        }

        var agora = relogio.GetUtcNow();
        await unidadeDeTrabalho.IniciarTransacao(cancellationToken);
        var numero = await gerador.Proximo(agora.Year, cancellationToken);
        var os = OrdemDeServico.Abrir(numero, cliente, veiculo, servicosSolicitados, comando.Autor, agora, pecasSolicitadas);

        ordens.Adicionar(os);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return OrdemDeServicoResumoDto.De(os);
    }

    private async Task<IReadOnlyList<Servico>> ServicosSolicitados(IReadOnlyCollection<Guid> servicoIds, CancellationToken cancellationToken)
    {
        var encontrados = (await servicos.ObterPorIds(servicoIds.Distinct().ToList(), cancellationToken)).ToDictionary(s => s.Id);

        return servicoIds
            .Select(id => encontrados.TryGetValue(id, out var servico)
                ? servico
                : throw new RecursoNaoEncontradoException("Servico nao encontrado."))
            .ToList();
    }

    private async Task<IReadOnlyList<PecaInsumoSolicitada>> PecasInsumosSolicitadas(
        IReadOnlyCollection<PecaInsumoNaAbertura> solicitadas,
        CancellationToken cancellationToken)
    {
        var resultado = new List<PecaInsumoSolicitada>();
        foreach (var solicitada in solicitadas)
        {
            var peca = await PecasInsumosDaOs.ObterOuFalhar(pecas, solicitada.PecaInsumoId, cancellationToken);
            resultado.Add(new PecaInsumoSolicitada(peca, Quantidade.Criar(solicitada.Quantidade, peca.Tipo)));
        }

        return resultado;
    }
}
