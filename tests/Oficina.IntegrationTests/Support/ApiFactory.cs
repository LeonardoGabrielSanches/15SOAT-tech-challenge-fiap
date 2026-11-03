using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Oficina.Application.Common;

namespace Oficina.IntegrationTests.Support;

public sealed class ApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public const string Esquema = "Teste";
    public const string CabecalhoPapel = "X-Teste-Papel";

    public Mock<INotificador> Notificador { get; } = new();

    public IReadOnlyList<OrcamentoDaNotificacao> OrcamentosNotificados() =>
        Notificador.Invocations
            .Where(i => i.Method.Name == nameof(INotificador.NotificarMudancaDeStatus))
            .SelectMany(i => i.Arguments.OfType<OrcamentoDaNotificacao>())
            .ToList();

    public string UltimoTokenNotificado() => OrcamentosNotificados()[^1].Token;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Oficina", postgres.ConnectionString("oficina_api"));
        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(opcoes =>
                {
                    opcoes.DefaultAuthenticateScheme = Esquema;
                    opcoes.DefaultChallengeScheme = Esquema;
                    opcoes.DefaultScheme = Esquema;
                })
                .AddScheme<AuthenticationSchemeOptions, ManipuladorDeAutenticacaoDeTeste>(Esquema, _ => { });
            services.RemoveAll<INotificador>();
            services.AddSingleton(Notificador.Object);
        });
    }

    public HttpClient CriarClienteComPapel(string papel)
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Add(CabecalhoPapel, papel);
        return cliente;
    }

    private sealed class ManipuladorDeAutenticacaoDeTeste(
        IOptionsMonitor<AuthenticationSchemeOptions> opcoes,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(opcoes, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(CabecalhoPapel, out var papel))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identidade = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "usuario-de-teste"), new Claim(ClaimTypes.Role, papel.ToString())],
                Esquema);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
