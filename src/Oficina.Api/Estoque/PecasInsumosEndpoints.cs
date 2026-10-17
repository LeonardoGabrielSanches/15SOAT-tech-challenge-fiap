using System.Text.Json.Serialization;
using Oficina.Api.Autenticacao;
using Oficina.Application.Estoque;
using Oficina.Domain.Estoque;
using Oficina.Infrastructure.Persistencia;

namespace Oficina.Api.Estoque;

public sealed record EntradaDeMercadoriaRequisicao(decimal Quantidade);

public static class PecasInsumosEndpoints
{
    public static IServiceCollection AddPecasInsumos(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddScoped<IRepositorioDePecaInsumo, RepositorioDePecaInsumo>();
        services.AddScoped<CadastrarPecaInsumoCasoDeUso>();
        services.AddScoped<AtualizarPecaInsumoCasoDeUso>();
        services.AddScoped<InativarPecaInsumoCasoDeUso>();
        services.AddScoped<RegistrarEntradaDeMercadoriaCasoDeUso>();
        services.AddScoped<ConsultarPecasInsumosCasoDeUso>();
        return services;
    }

    public static IEndpointRouteBuilder MapPecasInsumos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/pecas-insumos")
            .WithTags("Peças/insumos")
            .RequireAuthorization(Politicas.Gerente);

        grupo.MapPost("/", async (CadastrarPecaInsumo comando, CadastrarPecaInsumoCasoDeUso caso, CancellationToken ct) =>
        {
            var dto = await caso.Executar(comando, ct);
            return Results.Created($"/api/pecas-insumos/{dto.Id}", dto);
        });

        grupo.MapGet("/", async (bool? incluirInativos, ConsultarPecasInsumosCasoDeUso caso, CancellationToken ct) =>
            Results.Ok(await caso.Listar(incluirInativos ?? false, ct)));

        grupo.MapGet("/{id:guid}", async (Guid id, ConsultarPecasInsumosCasoDeUso caso, CancellationToken ct) =>
            Results.Ok(await caso.Obter(id, ct)));

        grupo.MapPut("/{id:guid}", async (Guid id, AtualizarPecaInsumo comando, AtualizarPecaInsumoCasoDeUso caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(id, comando, ct)));

        grupo.MapDelete("/{id:guid}", async (Guid id, InativarPecaInsumoCasoDeUso caso, CancellationToken ct) =>
        {
            await caso.Executar(id, ct);
            return Results.NoContent();
        });

        grupo.MapPost("/{id:guid}/entradas", async (Guid id, EntradaDeMercadoriaRequisicao corpo, RegistrarEntradaDeMercadoriaCasoDeUso caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(id, corpo.Quantidade, ct)));

        grupo.MapGet("/{id:guid}/saldo", async (Guid id, ConsultarPecasInsumosCasoDeUso caso, CancellationToken ct) =>
            Results.Ok(await caso.ConsultarSaldo(id, ct)));

        return app;
    }
}
