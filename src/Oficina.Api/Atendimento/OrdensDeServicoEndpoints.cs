using System.Security.Claims;
using Oficina.Api.Autenticacao;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Common;

namespace Oficina.Api.Atendimento;

public sealed record AbrirOsRequest(Guid ClienteId, Guid VeiculoId, IReadOnlyCollection<Guid>? ServicoIds);

public sealed record IncluirServicoNaOsRequest(Guid ServicoId);

public static class OrdensDeServicoEndpoints
{
    public static IServiceCollection AddOrdensDeServico(this IServiceCollection services)
    {
        services.AddScoped<AbrirOs>();
        services.AddScoped<IncluirServicoNaOs>();
        services.AddScoped<RemoverServicoDaOs>();
        services.AddScoped<ListarOs>();
        services.AddScoped<DetalharOs>();
        return services;
    }

    public static RouteGroupBuilder MapOrdensDeServico(this RouteGroupBuilder administrativo)
    {
        var ordens = administrativo.MapGroup("/ordens-de-servico").WithTags("Ordens de servico");

        ordens.MapPost("/", async (AbrirOsRequest requisicao, AbrirOs caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                var os = await caso.Executar(
                    new AbrirOsComando(requisicao.ClienteId, requisicao.VeiculoId, requisicao.ServicoIds ?? [], Autor(usuario)),
                    ct);
                return Results.Created($"/api/administrativo/ordens-de-servico/{os.Id}", os);
            })
            .RequireAuthorization(Politicas.Atendente);

        ordens.MapGet("/", async (string? status, ListarOs caso, CancellationToken ct) =>
                Results.Ok(await caso.Executar(InterpretarStatus(status), ct)))
            .RequireAuthorization(Politicas.Atendente);

        ordens.MapGet("/{id:guid}", async (Guid id, DetalharOs caso, CancellationToken ct) =>
                Results.Ok(await caso.Executar(id, ct)))
            .RequireAuthorization(Politicas.Atendente);

        ordens.MapPost("/{id:guid}/servicos", async (Guid id, IncluirServicoNaOsRequest requisicao, IncluirServicoNaOs caso, CancellationToken ct) =>
            {
                await caso.Executar(id, requisicao.ServicoId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Atendente);

        ordens.MapDelete("/{id:guid}/servicos/{servicoId:guid}", async (Guid id, Guid servicoId, RemoverServicoDaOs caso, CancellationToken ct) =>
            {
                await caso.Executar(id, servicoId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Atendente);

        return administrativo;
    }

    private static string Autor(ClaimsPrincipal usuario) =>
        usuario.FindFirst("preferred_username")?.Value ?? usuario.Identity?.Name ?? "desconhecido";

    private static StatusOs? InterpretarStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse<StatusOs>(status, ignoreCase: true, out var valor) && Enum.IsDefined(valor)
            ? valor
            : throw new DadosInvalidosException("Status invalido.");
    }
}
