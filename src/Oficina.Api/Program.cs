using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Oficina.Api.Atendimento;
using Oficina.Api.Autenticacao;
using Oficina.Api.Erros;
using Oficina.Api.Estoque;
using Oficina.Api.OpenApi;
using Oficina.Api.Servicos;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Oficina")
    ?? throw new InvalidOperationException("ConnectionStrings:Oficina nao configurada.");
var keycloak = builder.Configuration.GetSection(OpcoesDoKeycloak.Secao).Get<OpcoesDoKeycloak>()
    ?? throw new InvalidOperationException("Secao Keycloak nao configurada.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddCadastroDeClientes();
builder.Services.AddKeycloakAuthentication(keycloak);
builder.Services.AddPecasInsumos();
builder.Services.AddCatalogoDeServicos();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeExcecoesDeDominio>();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer(new TransformadorDeSeguranca(keycloak)));

var app = builder.Build();

await using (var escopo = app.Services.CreateAsyncScope())
{
    await escopo.ServiceProvider.GetRequiredService<OficinaDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Oficina API v1");
    o.OAuthClientId(keycloak.ClienteSwagger);
    o.OAuthUsePkce();
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapGet("/saude", async (OficinaDbContext contexto, CancellationToken ct) =>
        await contexto.Database.CanConnectAsync(ct) ? Results.Ok() : Results.StatusCode(503))
    .AllowAnonymous()
    .ExcludeFromDescription();

var administrativo = app.MapGroup("/api/administrativo");

administrativo.MapGet("/ping", () => Results.Ok(new { status = "ok" }))
    .RequireAuthorization(Politicas.Gerente);

administrativo.MapGet("/politicas/{nome}", async (string nome, HttpContext http, IAuthorizationService autorizacao) =>
{
    if (!Politicas.Todas.Contains(nome))
    {
        return Results.NotFound();
    }

    var resultado = await autorizacao.AuthorizeAsync(http.User, nome);
    return resultado.Succeeded ? Results.Ok(new { politica = nome }) : Results.Forbid();
}).RequireAuthorization();

app.MapPecasInsumos();
administrativo.MapClientes();
app.MapCatalogoDeServicos();

app.Run();

public partial class Program;
