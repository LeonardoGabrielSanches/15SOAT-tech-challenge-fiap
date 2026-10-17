using Oficina.Api.Autenticacao;
using Oficina.Application.Atendimento.UseCases;

namespace Oficina.Api.Atendimento;

public sealed record CadastrarClienteRequest(string? Nome, string? Documento, string? Email, string? Telefone);

public sealed record AtualizarClienteRequest(string? Nome, string? Email, string? Telefone);

public static class ClientesEndpoints
{
    public static IServiceCollection AddCadastroDeClientes(this IServiceCollection services)
    {
        services.AddScoped<CadastrarCliente>();
        services.AddScoped<ConsultarCliente>();
        services.AddScoped<ListarClientes>();
        services.AddScoped<AtualizarCliente>();
        services.AddScoped<InativarCliente>();
        return services;
    }

    public static RouteGroupBuilder MapClientes(this RouteGroupBuilder administrativo)
    {
        var clientes = administrativo.MapGroup("/clientes")
            .WithTags("Clientes")
            .RequireAuthorization(Politicas.Atendente);

        clientes.MapPost("/", async (CadastrarClienteRequest requisicao, CadastrarCliente caso, CancellationToken ct) =>
        {
            var cliente = await caso.Executar(
                new CadastrarClienteComando(requisicao.Nome, requisicao.Documento, requisicao.Email, requisicao.Telefone),
                ct);
            return Results.Created($"/api/administrativo/clientes/{cliente.Id}", cliente);
        });

        clientes.MapGet("/", async (bool? ativo, string? documento, ListarClientes caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(new ListarClientesFiltro(ativo, documento), ct)));

        clientes.MapGet("/{id:guid}", async (Guid id, ConsultarCliente caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(id, ct)));

        clientes.MapPut("/{id:guid}", async (Guid id, AtualizarClienteRequest requisicao, AtualizarCliente caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(
                new AtualizarClienteComando(id, requisicao.Nome, requisicao.Email, requisicao.Telefone),
                ct)));

        clientes.MapDelete("/{id:guid}", async (Guid id, InativarCliente caso, CancellationToken ct) =>
        {
            await caso.Executar(id, ct);
            return Results.NoContent();
        });

        return administrativo;
    }
}
