using System.Security.Claims;
using Oficina.Api.Autenticacao;
using Oficina.Application.Atendimento.Diagnostico;
using Oficina.Application.Atendimento.Execucao;
using Oficina.Application.Atendimento.Notificacao;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.Common;

namespace Oficina.Api.Atendimento;

public sealed record AbrirOsRequest(
    Guid ClienteId,
    Guid VeiculoId,
    IReadOnlyCollection<Guid>? ServicoIds,
    IReadOnlyCollection<PecaInsumoNaAberturaRequest>? PecasInsumos = null);

public sealed record PecaInsumoNaAberturaRequest(Guid PecaInsumoId, decimal Quantidade);

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
        services.AddScoped<IManipuladorDeEvento<StatusDaOsAlterado>, NotificarClienteDaMudancaDeStatus>();
        return services;
    }

    public static RouteGroupBuilder MapOrdensDeServico(this RouteGroupBuilder administrativo)
    {
        var ordens = administrativo.MapGroup("/ordens-de-servico").WithTags("Ordens de servico");

        ordens.MapPost("/", async (AbrirOsRequest requisicao, AbrirOs caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                var pecasInsumos = (requisicao.PecasInsumos ?? [])
                    .Select(p => new PecaInsumoNaAbertura(p.PecaInsumoId, p.Quantidade))
                    .ToList();
                var os = await caso.Executar(
                    new AbrirOsComando(requisicao.ClienteId, requisicao.VeiculoId, requisicao.ServicoIds ?? [], pecasInsumos, Autor(usuario)),
                    ct);
                return Results.Created($"/api/administrativo/ordens-de-servico/{os.Id}", os);
            })
            .RequireAuthorization(Politicas.Atendente);

        ordens.MapGet("/", async (
                    ListarOs caso,
                    CancellationToken ct,
                    string? status,
                    int pagina = ListarOs.PaginaPadrao,
                    int tamanho = ListarOs.TamanhoPadrao) =>
                Results.Ok(await caso.Executar(InterpretarStatus(status), pagina, tamanho, ct)))
            .RequireAuthorization(Politicas.AtendenteOuMecanico);

        ordens.MapGet("/{id:guid}", async (Guid id, DetalharOs caso, CancellationToken ct) =>
                Results.Ok(await caso.Executar(id, ct)))
            .RequireAuthorization(Politicas.AtendenteOuMecanico);

        ordens.MapPost("/{id:guid}/servicos", async (Guid id, IncluirServicoNaOsRequest requisicao, IncluirServicoNaOs caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                await caso.Executar(id, requisicao.ServicoId, Papel(usuario), ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.AtendenteOuMecanico);

        ordens.MapDelete("/{id:guid}/servicos/{servicoId:guid}", async (Guid id, Guid servicoId, RemoverServicoDaOs caso, ClaimsPrincipal usuario, CancellationToken ct) =>
            {
                await caso.Executar(id, servicoId, Papel(usuario), ct);
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

        ordens.MapGet("/tempo-medio-de-execucao", async (ConsultarTempoMedioDeExecucao caso, CancellationToken ct) =>
                Results.Ok(await caso.Executar(ct)))
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

    private static PapelDoUsuario Papel(ClaimsPrincipal usuario)
    {
        var papel = PapelDoUsuario.Nenhum;
        if (usuario.IsInRole(Politicas.Atendente)) papel |= PapelDoUsuario.Atendente;
        if (usuario.IsInRole(Politicas.Mecanico)) papel |= PapelDoUsuario.Mecanico;
        if (usuario.IsInRole(Politicas.Gerente)) papel |= PapelDoUsuario.Gerente;
        return papel;
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
