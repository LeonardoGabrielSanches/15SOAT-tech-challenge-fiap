using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.Clientes;

public sealed record CadastrarClienteComando(string? Nome, string? Documento, string? Email, string? Telefone);

public sealed class CadastrarCliente(IClienteRepository clientes, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<ClienteDto> Executar(CadastrarClienteComando comando, CancellationToken cancellationToken)
    {
        var documento = Documento.Criar(comando.Documento);
        var email = Email.Criar(comando.Email);
        var cliente = Cliente.Cadastrar(comando.Nome, documento, email, comando.Telefone);

        if (await clientes.ObterPorDocumento(documento, cancellationToken) is not null)
        {
            throw new ConflitoDeEstadoException("Ja existe um Cliente cadastrado com este documento.");
        }

        clientes.Adicionar(cliente);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return ClienteDto.De(cliente);
    }
}
