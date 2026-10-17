using Oficina.Application.Atendimento.UseCases;

namespace Oficina.Api.Atendimento;

public sealed record ConsultarOsPublicaRequest(string? Numero, string? Documento);

public static class ConsultaPublicaEndpoints
{
    public static IServiceCollection AddConsultaPublica(this IServiceCollection services)
    {
        services.AddScoped<ConsultarOsPublica>();
        return services;
    }

    public static IEndpointRouteBuilder MapConsultaPublica(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/publico/ordens-de-servico/consulta",
                async (ConsultarOsPublicaRequest requisicao, ConsultarOsPublica caso, CancellationToken ct) =>
                    Results.Ok(await caso.Executar(requisicao.Numero, requisicao.Documento, ct)))
            .WithTags("Consulta publica")
            .AllowAnonymous();

        return app;
    }
}
