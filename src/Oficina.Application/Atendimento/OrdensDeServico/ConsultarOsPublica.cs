using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed class ConsultarOsPublica(IOrdemDeServicoRepository ordens, IClienteRepository clientes)
{
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
                h.Autor == AutorDoHistorico.Cliente ? AutorDoHistorico.Cliente : AutorOficina))
            .ToList();

        return new ConsultaPublicaDaOsDto(os.Numero.Valor, os.Status.ToString(), os.AbertaEm, historico, OrcamentoVigenteDe(os));
    }

    private static OrcamentoPublicoDto? OrcamentoVigenteDe(OrdemDeServico os) =>
        os.VersaoVigente is { } v
            ? new OrcamentoPublicoDto(
                v.Numero,
                v.Situacao.ToString(),
                v.Itens.Select(i => new ItemDoOrcamentoPublicoDto(i.Descricao, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList(),
                v.Total)
            : null;

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
