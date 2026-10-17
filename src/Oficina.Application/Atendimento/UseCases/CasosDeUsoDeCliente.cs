using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.UseCases;

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

public sealed class ConsultarCliente(IClienteRepository clientes)
{
    public async Task<ClienteDto> Executar(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Cliente nao encontrado.");
        return ClienteDto.De(cliente);
    }
}

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

public sealed record AtualizarClienteComando(Guid Id, string? Nome, string? Email, string? Telefone);

public sealed class AtualizarCliente(IClienteRepository clientes, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<ClienteDto> Executar(AtualizarClienteComando comando, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorId(comando.Id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Cliente nao encontrado.");

        cliente.AtualizarDados(comando.Nome, Email.Criar(comando.Email), comando.Telefone);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return ClienteDto.De(cliente);
    }
}

public sealed class InativarCliente(IClienteRepository clientes, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Cliente nao encontrado.");

        cliente.Inativar();

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}
