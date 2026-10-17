using Oficina.Api.Autenticacao;
using Oficina.Application.Atendimento.UseCases;

namespace Oficina.Api.Atendimento;

public sealed record CadastrarVeiculoRequest(string? Placa, string? Marca, string? Modelo, int AnoFabricacao, Guid ProprietarioId);

public sealed record AtualizarVeiculoRequest(string? Marca, string? Modelo, int AnoFabricacao);

public sealed record TransferirVeiculoRequest(Guid NovoProprietarioId);

public static class VeiculosEndpoints
{
    public static IServiceCollection AddCadastroDeVeiculos(this IServiceCollection services)
    {
        services.AddScoped<CadastrarVeiculo>();
        services.AddScoped<ConsultarVeiculo>();
        services.AddScoped<ListarVeiculosDoCliente>();
        services.AddScoped<AtualizarVeiculo>();
        services.AddScoped<InativarVeiculo>();
        services.AddScoped<TransfereVeiculo>();
        return services;
    }

    public static RouteGroupBuilder MapVeiculos(this RouteGroupBuilder administrativo)
    {
        var veiculos = administrativo.MapGroup("/veiculos")
            .WithTags("Veiculos")
            .RequireAuthorization(Politicas.Atendente);

        veiculos.MapPost("/", async (CadastrarVeiculoRequest requisicao, CadastrarVeiculo caso, CancellationToken ct) =>
        {
            var veiculo = await caso.Executar(
                new CadastrarVeiculoComando(requisicao.Placa, requisicao.Marca, requisicao.Modelo, requisicao.AnoFabricacao, requisicao.ProprietarioId),
                ct);
            return Results.Created($"/api/administrativo/veiculos/{veiculo.Id}", veiculo);
        });

        veiculos.MapGet("/{id:guid}", async (Guid id, ConsultarVeiculo caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(id, ct)));

        veiculos.MapPut("/{id:guid}", async (Guid id, AtualizarVeiculoRequest requisicao, AtualizarVeiculo caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(
                new AtualizarVeiculoComando(id, requisicao.Marca, requisicao.Modelo, requisicao.AnoFabricacao),
                ct)));

        veiculos.MapDelete("/{id:guid}", async (Guid id, InativarVeiculo caso, CancellationToken ct) =>
        {
            await caso.Executar(id, ct);
            return Results.NoContent();
        });

        veiculos.MapPost("/{id:guid}/transferencia", async (Guid id, TransferirVeiculoRequest requisicao, TransfereVeiculo caso, CancellationToken ct) =>
            Results.Ok(await caso.Executar(new TransferirVeiculoComando(id, requisicao.NovoProprietarioId), ct)));

        administrativo.MapGet("/clientes/{clienteId:guid}/veiculos",
                async (Guid clienteId, bool? ativo, ListarVeiculosDoCliente caso, CancellationToken ct) =>
                    Results.Ok(await caso.Executar(clienteId, ativo, ct)))
            .WithTags("Veiculos")
            .RequireAuthorization(Politicas.Atendente);

        return administrativo;
    }
}
