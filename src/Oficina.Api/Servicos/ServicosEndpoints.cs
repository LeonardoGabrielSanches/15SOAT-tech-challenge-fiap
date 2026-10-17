using Oficina.Api.Autenticacao;
using Oficina.Application.CatalogoEstoque.UseCases;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Infrastructure.Persistencia;

namespace Oficina.Api.Servicos;

public static class ServicosEndpoints
{
    public static IServiceCollection AddCatalogoDeServicos(this IServiceCollection services)
    {
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<CadastrarServico>();
        services.AddScoped<ConsultarServico>();
        services.AddScoped<ListarServicos>();
        services.AddScoped<AtualizarServico>();
        services.AddScoped<InativarServico>();
        return services;
    }

    public static IEndpointRouteBuilder MapCatalogoDeServicos(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/administrativo/servicos").WithTags("Servicos");

        grupo.MapGet("/", async (ListarServicos caso, CancellationToken ct) => Results.Ok(await caso.Executar(ct)))
            .RequireAuthorization(PoliticaDeLeitura);

        grupo.MapGet("/{id:guid}", async (Guid id, ConsultarServico caso, CancellationToken ct) =>
                Results.Ok(await caso.Executar(id, ct)))
            .RequireAuthorization(PoliticaDeLeitura);

        grupo.MapPost("/", async (DadosDoServico dados, CadastrarServico caso, CancellationToken ct) =>
            {
                var servico = await caso.Executar(dados, ct);
                return Results.Created($"/api/administrativo/servicos/{servico.Id}", servico);
            })
            .RequireAuthorization(Politicas.Gerente);

        grupo.MapPut("/{id:guid}", async (Guid id, DadosDoServico dados, AtualizarServico caso, CancellationToken ct) =>
                Results.Ok(await caso.Executar(id, dados, ct)))
            .RequireAuthorization(Politicas.Gerente);

        grupo.MapDelete("/{id:guid}", async (Guid id, InativarServico caso, CancellationToken ct) =>
            {
                await caso.Executar(id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Politicas.Gerente);

        return rotas;
    }

    private static void PoliticaDeLeitura(Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder politica) =>
        politica.RequireRole(Politicas.Atendente, Politicas.Mecanico, Politicas.Gerente);
}
