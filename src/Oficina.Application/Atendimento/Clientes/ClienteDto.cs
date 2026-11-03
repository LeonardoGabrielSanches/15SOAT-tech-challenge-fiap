using Oficina.Domain.Atendimento.Entities;

namespace Oficina.Application.Atendimento.Clientes;

public sealed record ClienteDto(
    Guid Id,
    string Nome,
    string TipoDocumento,
    string Documento,
    string Email,
    string? Telefone,
    bool Ativo)
{
    public static ClienteDto De(Cliente cliente) => new(
        cliente.Id,
        cliente.Nome,
        cliente.Documento.Tipo.ToString(),
        cliente.Documento.Numero,
        cliente.Email.Endereco,
        cliente.Telefone,
        cliente.Ativo);
}
