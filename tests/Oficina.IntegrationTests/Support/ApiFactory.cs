using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Oficina.Application.Common;
using Oficina.Infrastructure.Persistencia;

namespace Oficina.IntegrationTests.Support;

public sealed class ApiFactory(PostgresFixture postgres, bool bancoInacessivel = false) : WebApplicationFactory<Program>
{
    private const string StringDeConexaoInacessivel =
        "Host=127.0.0.1;Port=1;Database=oficina;Username=oficina;Password=oficina;Timeout=2;Command Timeout=2";

    public const string Esquema = "Teste";
    public const string CabecalhoPapel = "X-Teste-Papel";

    public static ApiFactory ComBancoInacessivel(PostgresFixture postgres) => new(postgres, bancoInacessivel: true);

    public Mock<INotificador> Notificador { get; } = new();

    public IReadOnlyList<NotificacaoDeStatus> NotificacoesEnviadas() =>
        Notificador.Invocations
            .Where(i => i.Method.Name == nameof(INotificador.NotificarMudancaDeStatus))
            .SelectMany(i => i.Arguments.OfType<NotificacaoDeStatus>())
            .ToList();

    public IReadOnlyList<OrcamentoDaNotificacao> OrcamentosNotificados() =>
        NotificacoesEnviadas()
            .Select(n => n.Orcamento)
            .OfType<OrcamentoDaNotificacao>()
            .ToList();

    public string UltimoTokenNotificado() => OrcamentosNotificados()[^1].Token;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:Oficina",
            bancoInacessivel ? StringDeConexaoInacessivel : postgres.ConnectionString("oficina_api"));
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

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        if (!bancoInacessivel)
        {
            using var escopo = host.Services.CreateScope();
            escopo.ServiceProvider.GetRequiredService<OficinaDbContext>().Database.Migrate();
        }

        return host;
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
