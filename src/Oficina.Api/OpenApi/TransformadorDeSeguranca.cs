using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Oficina.Api.Autenticacao;

namespace Oficina.Api.OpenApi;

public sealed class TransformadorDeSeguranca(OpcoesDoKeycloak keycloak) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument documento, OpenApiDocumentTransformerContext contexto, CancellationToken cancellationToken)
    {
        var baseUrl = $"{keycloak.UrlPublica.TrimEnd('/')}/protocol/openid-connect";
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["oauth2"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{baseUrl}/auth"),
                        TokenUrl = new Uri($"{baseUrl}/token"),
                        Scopes = new Dictionary<string, string> { ["openid"] = "OpenID Connect" }
                    }
                }
            }
        };
        documento.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("oauth2", documento)] = ["openid"]
            }
        ];
        return Task.CompletedTask;
    }
}
