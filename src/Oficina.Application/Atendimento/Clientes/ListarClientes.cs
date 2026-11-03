using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;

namespace Oficina.Application.Atendimento.Clientes;

public sealed record ListarClientesFiltro(bool? Ativo, string? Documento);

public sealed class ListarClientes(IClienteRepository clientes)
{
    public async Task<IReadOnlyList<ClienteDto>> Executar(ListarClientesFiltro filtro, CancellationToken cancellationToken)
    {
        var documento = string.IsNullOrWhiteSpace(filtro.Documento) ? null : Documento.Criar(filtro.Documento);
        var encontrados = await clientes.Listar(filtro.Ativo, documento, cancellationToken);
        return encontrados.Select(ClienteDto.De).ToList();
    }
}
