using Oficina.Application.Atendimento.UseCases;

namespace Oficina.Api.Atendimento;

public sealed record RecusarOrcamentoRequest(string? Motivo);

public static class AprovacaoEndpoints
{
    public static IServiceCollection AddAprovacao(this IServiceCollection services)
    {
        services.AddScoped<AprovarOrcamento>();
        services.AddScoped<RecusarOrcamento>();
        return services;
    }

    public static IEndpointRouteBuilder MapAprovacao(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/publico/orcamentos").WithTags("Aprovacao do orcamento").AllowAnonymous();

        grupo.MapPost("/{token}/aprovar", async (string token, AprovarOrcamento caso, CancellationToken ct) =>
        {
            await caso.Executar(token, ct);
            return Results.NoContent();
        });

        grupo.MapPost("/{token}/recusar", async (string token, RecusarOrcamentoRequest? requisicao, RecusarOrcamento caso, CancellationToken ct) =>
        {
            await caso.Executar(token, requisicao?.Motivo, ct);
            return Results.NoContent();
        });

        return app;
    }
}
