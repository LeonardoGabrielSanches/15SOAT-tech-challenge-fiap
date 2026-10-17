using System.Security.Claims;
using Oficina.Api.Autenticacao;
using Oficina.Application.Atendimento.UseCases;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.Common;

namespace Oficina.Api.Atendimento;

public sealed record AbrirOsRequest(Guid ClienteId, Guid VeiculoId, IReadOnlyCollection<Guid>? ServicoIds);

public sealed record IncluirServicoNaOsRequest(Guid ServicoId);

public sealed record IncluirPecaInsumoNaOsRequest(Guid PecaInsumoId, decimal Quantidade);

public sealed record CancelarOsRequest(string? Motivo);

public static class OrdensDeServicoEndpoints
{
    public static IServiceCollection AddOrdensDeServico(this IServiceCollection services)
    {
        services.AddScoped<AbrirOs>();
        services.AddScoped<IncluirServicoNaOs>();
        services.AddScoped<RemoverServicoDaOs>();
        services.AddScoped<ListarOs>();
        services.AddScoped<DetalharOs>();
        services.AddScoped<IniciarDiagnostico>();
        services.AddScoped<IncluirPecaInsumoNaOs>();
        services.AddScoped<RemoverPecaInsumoDaOs>();
        services.AddScoped<ConcluirDiagnostico>();
        services.AddScoped<ListarFilaDeExecucao>();
        services.AddScoped<IniciarExecucao>();
        services.AddScoped<FinalizarExecucao>();
        services.AddScoped<RegistrarEntrega>();
        services.AddScoped<CancelarOs>();
        services.AddScoped<ConsultarTempoMedioDeExecucao>();
        services.AddScoped<IManipuladorDeEvento<DiagnosticoConcluido>, EnviarOrcamentoAoConcluirDiagnostico>();
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
            .RequireAuthorization(Politicas.AtendenteOuMecanico);

        ordens.MapPost("/{id:guid}/servicos", async (Guid id, IncluirServicoNaOsRequest requisicao, IncluirServicoNaOs caso, CancellationToken ct) =>
            {
                await caso.Executar(id, requisicao.ServicoId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.AtendenteOuMecanico);

        ordens.MapDelete("/{id:guid}/servicos/{servicoId:guid}", async (Guid id, Guid servicoId, RemoverServicoDaOs caso, CancellationToken ct) =>
            {
                await caso.Executar(id, servicoId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.AtendenteOuMecanico);

        ordens.MapPost("/{id:guid}/diagnostico/iniciar", async (Guid id, IniciarDiagnostico caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                await caso.Executar(id, Autor(usuario), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Mecanico);

        ordens.MapPost("/{id:guid}/diagnostico/concluir", async (Guid id, ConcluirDiagnostico caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                await caso.Executar(id, Autor(usuario), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Mecanico);

        ordens.MapPost("/{id:guid}/cancelar", async (Guid id, CancelarOsRequest requisicao, CancelarOs caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                await caso.Executar(id, requisicao.Motivo ?? string.Empty, Autor(usuario), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Atendente);

        ordens.MapPost("/{id:guid}/pecas-insumos", async (Guid id, IncluirPecaInsumoNaOsRequest requisicao, IncluirPecaInsumoNaOs caso, CancellationToken ct) =>
            {
                await caso.Executar(id, requisicao.PecaInsumoId, requisicao.Quantidade, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Mecanico);

        ordens.MapDelete("/{id:guid}/pecas-insumos/{pecaInsumoId:guid}", async (Guid id, Guid pecaInsumoId, decimal? quantidade, RemoverPecaInsumoDaOs caso, CancellationToken ct) =>
            {
                await caso.Executar(id, pecaInsumoId, quantidade, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Mecanico);

        ordens.MapGet("/fila-de-execucao", async (ListarFilaDeExecucao caso, CancellationToken ct) =>
                Results.Ok(await caso.Executar(ct)))
            .RequireAuthorization(Politicas.Mecanico);

        ordens.MapGet("/tempo-medio-de-execucao", async (Guid? servicoId, ConsultarTempoMedioDeExecucao caso, CancellationToken ct) =>
                Results.Ok(await caso.Executar(servicoId, ct)))
            .RequireAuthorization(Politicas.Gerente);

        ordens.MapPost("/{id:guid}/execucao/iniciar", async (Guid id, IniciarExecucao caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                await caso.Executar(id, Autor(usuario), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Mecanico);

        ordens.MapPost("/{id:guid}/execucao/finalizar", async (Guid id, FinalizarExecucao caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                await caso.Executar(id, Autor(usuario), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Mecanico);

        ordens.MapPost("/{id:guid}/entrega", async (Guid id, RegistrarEntrega caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                await caso.Executar(id, Autor(usuario), ct);
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
