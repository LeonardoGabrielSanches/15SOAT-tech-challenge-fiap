using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.UseCases;

public sealed record ItemDoOrcamentoPublicoDto(string Descricao, decimal Quantidade, decimal PrecoUnitario, decimal Subtotal);

public sealed record OrcamentoPublicoDto(int Versao, string Situacao, IReadOnlyList<ItemDoOrcamentoPublicoDto> Itens, decimal Total);

public sealed record EntradaDoHistoricoPublicoDto(string? Origem, string Destino, DateTimeOffset Momento, string Autor);

public sealed record ConsultaPublicaDaOsDto(
    string Numero,
    string Status,
    DateTimeOffset AbertaEm,
    IReadOnlyList<EntradaDoHistoricoPublicoDto> Historico,
    OrcamentoPublicoDto? OrcamentoVigente);

public sealed class ConsultarOsPublica(IOrdemDeServicoRepository ordens, IClienteRepository clientes)
{
    private const string AutorCliente = "Cliente";
    private const string AutorOficina = "Oficina";

    public async Task<ConsultaPublicaDaOsDto> Executar(string? numero, string? documento, CancellationToken cancellationToken)
    {
        var os = string.IsNullOrWhiteSpace(numero)
            ? null
            : await ordens.ObterPorNumero(numero.Trim().ToUpperInvariant(), cancellationToken);
        var cliente = os is null ? null : await clientes.ObterPorId(os.ClienteId, cancellationToken);

        if (os is null || cliente is null || !DocumentoConfere(cliente.Documento, documento))
        {
            throw new RecursoNaoEncontradoException("OS nao encontrada.");
        }

        var historico = os.Historico
            .Select(h => new EntradaDoHistoricoPublicoDto(
                h.Origem?.ToString(),
                h.Destino.ToString(),
                h.Momento,
                h.Autor == AutorCliente ? AutorCliente : AutorOficina))
            .ToList();

        return new ConsultaPublicaDaOsDto(os.Numero.Valor, os.Status.ToString(), os.AbertaEm, historico, null);
    }

    private static bool DocumentoConfere(Documento esperado, string? informado)
    {
        try
        {
            return Documento.Criar(informado) == esperado;
        }
        catch (DadosInvalidosException)
        {
            return false;
        }
    }
}
