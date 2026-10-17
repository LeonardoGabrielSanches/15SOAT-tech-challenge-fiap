using System.Text.Json.Serialization;
using Oficina.Api.Autenticacao;
using Oficina.Application.Estoque.UseCases;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;
using Oficina.Infrastructure.Persistencia.Estoque;

namespace Oficina.Api.Estoque;

public sealed record EntradaDeMercadoriaRequisicao(decimal Quantidade);

public static class PecasInsumosEndpoints
{
    public static IServiceCollection AddPecasInsumos(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddScoped<IPecaInsumoRepository, PecaInsumoRepository>();
        services.AddScoped<CadastrarPecaInsumo>();
        services.AddScoped<AtualizarPecaInsumo>();
        services.AddScoped<InativarPecaInsumo>();
        services.AddScoped<RegistrarEntradaDeMercadoria>();
        services.AddScoped<ConsultarPecasInsumos>();
        return services;
    }

    public static IEndpointRouteBuilder MapPecasInsumos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/pecas-insumos")
            .WithTags("Peças/insumos");

        grupo.MapPost("/", async (CadastrarPecaInsumoComando comando, CadastrarPecaInsumo caso, CancellationToken ct) =>
        {
            var dto = await caso.Executar(comando, ct);
            return Results.Created($"/api/pecas-insumos/{dto.Id}", dto);
        }).RequireAuthorization(Politicas.Gerente);

        grupo.MapGet("/", async (bool? incluirInativos, ConsultarPecasInsumos caso, CancellationToken ct) =>
            Results.Ok(await caso.Listar(incluirInativos ?? false, ct)))
            .RequireAuthorization(Politicas.Mecanico);

        grupo.MapGet("/{id:guid}", async (Guid id, ConsultarPecasInsumos caso, CancellationToken ct) =>
            Results.Ok(await caso.Obter(id, ct)))
            .RequireAuthorization(Politicas.Mecanico);

        grupo.MapPut("/{id:guid}", async (Guid id, AtualizarPecaInsumoComando comando, AtualizarPecaInsumo caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(id, comando, ct)))
            .RequireAuthorization(Politicas.Gerente);

        grupo.MapDelete("/{id:guid}", async (Guid id, InativarPecaInsumo caso, CancellationToken ct) =>
        {
            await caso.Executar(id, ct);
            return Results.NoContent();
        }).RequireAuthorization(Politicas.Gerente);

        grupo.MapPost("/{id:guid}/entradas", async (Guid id, EntradaDeMercadoriaRequisicao corpo, RegistrarEntradaDeMercadoria caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(id, corpo.Quantidade, ct)))
            .RequireAuthorization(Politicas.Gerente);

        grupo.MapGet("/{id:guid}/saldo", async (Guid id, ConsultarPecasInsumos caso, CancellationToken ct) =>
            Results.Ok(await caso.ConsultarSaldo(id, ct)))
            .RequireAuthorization(Politicas.Mecanico);

        return app;
    }
}
