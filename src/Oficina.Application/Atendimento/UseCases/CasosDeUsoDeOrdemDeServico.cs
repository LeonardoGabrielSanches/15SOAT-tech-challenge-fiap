using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Atendimento.UseCases;

public sealed record OrdemDeServicoResumoDto(
    Guid Id,
    string Numero,
    string Status,
    Guid ClienteId,
    Guid VeiculoId,
    DateTimeOffset AbertaEm)
{
    public static OrdemDeServicoResumoDto De(OrdemDeServico os) =>
        new(os.Id, os.Numero.Valor, os.Status.ToString(), os.ClienteId, os.VeiculoId, os.AbertaEm);
}

public sealed record ServicoDaOsDto(Guid ServicoId, string Nome, decimal Preco);

public sealed record ItemDePecaInsumoDaOsDto(Guid PecaInsumoId, string Nome, string UnidadeDeMedida, decimal Quantidade, decimal Preco);

public sealed record ItemDaVersaoDto(string Tipo, Guid ReferenciaId, string Descricao, decimal Quantidade, decimal PrecoUnitario, decimal Subtotal);

public sealed record VersaoDoOrcamentoDto(
    int Numero,
    decimal Total,
    string Situacao,
    DateTimeOffset GeradaEm,
    string? MotivoDaRecusa,
    IReadOnlyList<ItemDaVersaoDto> Itens);

public sealed record EntradaDoHistoricoDto(string? Origem, string Destino, DateTimeOffset Momento, string Autor);

public sealed record OrdemDeServicoDetalheDto(
    Guid Id,
    string Numero,
    string Status,
    DateTimeOffset AbertaEm,
    ClienteDto Cliente,
    VeiculoDto Veiculo,
    IReadOnlyList<ServicoDaOsDto> Servicos,
    IReadOnlyList<VersaoDoOrcamentoDto> VersoesDoOrcamento,
    IReadOnlyList<EntradaDoHistoricoDto> Historico,
    IReadOnlyList<ItemDePecaInsumoDaOsDto> PecasInsumos,
    string? MotivoDaUltimaRecusa);

internal static class BuscaDeOrdemDeServico
{
    public static async Task<OrdemDeServico> ObterOuFalhar(IOrdemDeServicoRepository ordens, Guid id, CancellationToken cancellationToken) =>
        await ordens.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("OS nao encontrada.");
}

public sealed record AbrirOsComando(Guid ClienteId, Guid VeiculoId, IReadOnlyCollection<Guid> ServicoIds, string Autor);

public sealed class AbrirOs(
    IOrdemDeServicoRepository ordens,
    IClienteRepository clientes,
    IVeiculoRepository veiculos,
    IServicoRepository servicos,
    IGeradorNumeroOs gerador,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<OrdemDeServicoResumoDto> Executar(AbrirOsComando comando, CancellationToken cancellationToken)
    {
        if (comando.ServicoIds.Count == 0)
        {
            throw new DadosInvalidosException("A OS deve ter ao menos um Servico.");
        }

        var idsDistintos = comando.ServicoIds.Distinct().ToList();
        if (idsDistintos.Count != comando.ServicoIds.Count)
        {
            throw new DadosInvalidosException("Servico repetido na OS.");
        }

        var cliente = await clientes.ObterPorId(comando.ClienteId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Cliente nao encontrado.");
        var veiculo = await veiculos.ObterPorId(comando.VeiculoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Veiculo nao encontrado.");
        var encontrados = await servicos.ObterPorIds(idsDistintos, cancellationToken);
        if (encontrados.Count != idsDistintos.Count)
        {
            throw new RecursoNaoEncontradoException("Servico nao encontrado.");
        }

        cliente.GarantirAtivo();
        veiculo.GarantirAtivo();

        if (veiculo.ProprietarioId != cliente.Id)
        {
            throw new DadosInvalidosException("O Cliente informado nao e o Proprietario atual do Veiculo.");
        }

        if (await ordens.ExisteEmAndamentoDoVeiculo(veiculo.Id, cancellationToken))
        {
            throw new ConflitoDeEstadoException("O Veiculo ja possui uma OS em andamento.");
        }

        var agora = relogio.GetUtcNow();
        var numero = await gerador.Proximo(agora.Year, cancellationToken);
        var os = OrdemDeServico.Abrir(numero, cliente, veiculo, encontrados.ToList(), comando.Autor, agora);

        ordens.Adicionar(os);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return OrdemDeServicoResumoDto.De(os);
    }
}

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

public sealed class IncluirServicoNaOs(IOrdemDeServicoRepository ordens, IServicoRepository servicos, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid osId, Guid servicoId, PapelDoUsuario papel, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        PermissaoDeServicosDaOs.Garantir(os, papel);
        var servico = await servicos.Obter(servicoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Servico nao encontrado.");

        os.IncluirServico(servico);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed class RemoverServicoDaOs(IOrdemDeServicoRepository ordens, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid osId, Guid servicoId, PapelDoUsuario papel, CancellationToken cancellationToken)
    {
        var os = await BuscaDeOrdemDeServico.ObterOuFalhar(ordens, osId, cancellationToken);
        PermissaoDeServicosDaOs.Garantir(os, papel);

        os.RemoverServico(servicoId);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed class ListarOs(IOrdemDeServicoRepository ordens)
{
    private static readonly StatusOs[] OrdemDePrioridade =
    [
        StatusOs.EmExecucao,
        StatusOs.Aprovada,
        StatusOs.AguardandoAprovacao,
        StatusOs.EmDiagnostico,
        StatusOs.Recebida,
        StatusOs.Finalizada
    ];

    public async Task<IReadOnlyList<OrdemDeServicoResumoDto>> Executar(StatusOs? status, CancellationToken cancellationToken)
    {
        var encontradas = await ordens.Listar(status, cancellationToken);

        return encontradas
            .OrderBy(o => Array.IndexOf(OrdemDePrioridade, o.Status) is var i and >= 0 ? i : OrdemDePrioridade.Length)
            .ThenBy(o => o.AbertaEm)
            .Select(OrdemDeServicoResumoDto.De)
            .ToList();
    }
}

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
        var veiculo = await veiculos.ObterPorId(os.VeiculoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Veiculo nao encontrado.");
        var catalogo = await servicos.ObterPorIds(os.Servicos.Select(s => s.ServicoId).ToList(), cancellationToken);

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

        var pecasInsumos = new List<ItemDePecaInsumoDaOsDto>();
        foreach (var item in os.ItensDePecaInsumo)
        {
            var peca = await BuscaDePecaInsumo.ObterOuFalhar(pecas, item.PecaInsumoId, cancellationToken);
            pecasInsumos.Add(new ItemDePecaInsumoDaOsDto(peca.Id, peca.Nome, peca.UnidadeDeMedida, item.Quantidade, peca.Preco.Valor));
        }

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
            pecasInsumos.OrderBy(p => p.Nome).ToList(),
            os.MotivoDaUltimaRecusa);
    }

    private static VersaoDoOrcamentoDto DeVersao(VersaoDoOrcamento v) =>
        new(
            v.Numero,
            v.Total,
            v.Situacao.ToString(),
            v.GeradaEm,
            v.MotivoDaRecusa,
            v.Itens.Select(i => new ItemDaVersaoDto(i.Tipo.ToString(), i.ReferenciaId, i.Descricao, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList());
}
