using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Oficina.Api.Autenticacao;

public static class AutenticacaoKeycloak
{
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, OpcoesDoKeycloak opcoes)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = opcoes.Authority;
                jwt.Audience = opcoes.Audience;
                jwt.RequireHttpsMetadata = opcoes.ExigirHttps;
                jwt.MapInboundClaims = false;
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = contexto =>
                    {
                        MapearPapeis(contexto.Principal);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Politicas.Atendente, p => p.RequireRole(Politicas.Atendente, Politicas.Gerente))
            .AddPolicy(Politicas.Mecanico, p => p.RequireRole(Politicas.Mecanico, Politicas.Gerente))
            .AddPolicy(Politicas.Gerente, p => p.RequireRole(Politicas.Gerente));

        return services;
    }

    private static void MapearPapeis(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identidade)
        {
            return;
        }

        var acesso = identidade.FindFirst("realm_access")?.Value;
        if (acesso is null)
        {
            return;
        }

        using var documento = JsonDocument.Parse(acesso);
        if (!documento.RootElement.TryGetProperty("roles", out var papeis))
        {
            return;
        }

        foreach (var papel in papeis.EnumerateArray())
        {
            identidade.AddClaim(new Claim(ClaimTypes.Role, papel.GetString()!));
        }
    }
}
