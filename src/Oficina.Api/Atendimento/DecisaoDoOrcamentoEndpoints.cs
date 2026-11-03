using System.Text.Json.Serialization;
using Oficina.Application.Atendimento.Orcamento;

namespace Oficina.Api.Atendimento;

public enum DecisaoDoOrcamento
{
    Aprovar,
    Recusar
}

public sealed class ConversorSomenteNomesDaDecisao() : JsonStringEnumConverter<DecisaoDoOrcamento>(null, allowIntegerValues: false);

public sealed record DecisaoDoOrcamentoRequest([property: JsonRequired, JsonConverter(typeof(ConversorSomenteNomesDaDecisao))] DecisaoDoOrcamento Decisao, string? Motivo = null);

public static class DecisaoDoOrcamentoEndpoints
{
    public static IServiceCollection AddDecisaoDoOrcamento(this IServiceCollection services)
    {
        services.AddScoped<AprovarOrcamento>();
        services.AddScoped<RecusarOrcamento>();
        return services;
    }

    public static IEndpointRouteBuilder MapDecisaoDoOrcamento(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/publico/orcamentos").WithTags("Decisao do orcamento").AllowAnonymous();

        grupo.MapPost("/{token}/decisao", async (
            string token,
            DecisaoDoOrcamentoRequest requisicao,
            AprovarOrcamento aprovar,
            RecusarOrcamento recusar,
            CancellationToken ct) =>
        {
            if (requisicao.Decisao == DecisaoDoOrcamento.Aprovar)
            {
                await aprovar.Executar(token, ct);
            }
            else
            {
                await recusar.Executar(token, requisicao.Motivo, ct);
            }

            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }
}
