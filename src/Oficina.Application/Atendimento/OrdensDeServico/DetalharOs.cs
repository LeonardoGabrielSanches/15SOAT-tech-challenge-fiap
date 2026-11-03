using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed class DetalharOs(
    IOrdemDeServicoRepository ordens,
    IClienteRepository clientes,
    IVeiculoRepository veiculos,
    IServicoRepository servicos,
    IPecaInsumoRepository pecas)
{
    public async Task<OrdemDeServicoDetalheDto> Executar(Guid id, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, id, cancellationToken);
        var cliente = await BuscaDeCliente.ObterOuFalhar(clientes, os.ClienteId, cancellationToken);
        var veiculo = await BuscaDeVeiculo.ObterOuFalhar(veiculos, os.VeiculoId, cancellationToken);
        var catalogo = await servicos.ObterPorIds(os.Servicos.Select(s => s.ServicoId).ToList(), cancellationToken);
        var pecasDaOs = (await PecasInsumosDaOs.Carregar(pecas, os, cancellationToken)).ToDictionary(p => p.Id);

        var congelados = os.VersaoVigente?.Itens
            .Where(i => i.Tipo == TipoDoItemDoOrcamento.Servico)
            .ToDictionary(i => i.ReferenciaId, i => i.PrecoUnitario)
            ?? [];
        var itens = catalogo
            .Select(s => new ServicoDaOsDto(s.Id, s.Nome, congelados.TryGetValue(s.Id, out var congelado) ? congelado : s.Preco.Valor))
            .OrderBy(s => s.Nome)
            .ToList();
        var historico = os.Historico
            .Select(h => new EntradaDoHistoricoDto(h.Origem?.ToString(), h.Destino.ToString(), h.Momento, h.Autor))
            .ToList();
        var pecasInsumos = os.ItensDePecaInsumo
            .Select(item => ItemDePecaInsumo(item, pecasDaOs[item.PecaInsumoId]))
            .OrderBy(p => p.Nome)
            .ToList();

        return new OrdemDeServicoDetalheDto(
            os.Id,
            os.Numero.Valor,
            os.Status.ToString(),
            os.AbertaEm,
            ClienteDto.De(cliente),
            VeiculoDto.De(veiculo),
            itens,
            os.Versoes.Select(DeVersao).ToList(),
            historico,
            pecasInsumos,
            os.MotivoDaUltimaRecusa);
    }

    private static ItemDePecaInsumoDaOsDto ItemDePecaInsumo(ItemDaOs item, PecaInsumo peca) =>
        new(peca.Id, peca.Nome, peca.UnidadeDeMedida, item.Quantidade, peca.Preco.Valor);

    private static VersaoDoOrcamentoDto DeVersao(VersaoDoOrcamento v) =>
        new(
            v.Numero,
            v.Total,
            v.Situacao.ToString(),
            v.GeradaEm,
            v.MotivoDaRecusa,
            v.Itens.Select(i => new ItemDaVersaoDto(i.Tipo.ToString(), i.ReferenciaId, i.Descricao, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList());
}
