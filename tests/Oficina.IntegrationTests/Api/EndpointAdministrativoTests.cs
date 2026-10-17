using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Common;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api;

[Collection(PostgresCollection.Nome)]
public class EndpointAdministrativoTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Sem_token_responde_401()
    {
        var resposta = await _factory.CreateClient().GetAsync("/api/administrativo/ping");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Theory]
    [InlineData("atendente")]
    [InlineData("mecanico")]
    [InlineData("cliente")]
    public async Task Com_papel_errado_responde_403(string papel)
    {
        var resposta = await _factory.CriarClienteComPapel(papel).GetAsync("/api/administrativo/ping");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Gerente_acessa_o_endpoint()
    {
        var resposta = await _factory.CriarClienteComPapel("gerente").GetAsync("/api/administrativo/ping");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("atendente")]
    [InlineData("mecanico")]
    [InlineData("gerente")]
    public async Task Politica_aceita_o_proprio_papel(string papel)
    {
        var resposta = await _factory.CriarClienteComPapel(papel).GetAsync($"/api/administrativo/politicas/{papel}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("atendente")]
    [InlineData("mecanico")]
    [InlineData("gerente")]
    public async Task Gerente_passa_em_todas_as_politicas(string politica)
    {
        var resposta = await _factory.CriarClienteComPapel("gerente").GetAsync($"/api/administrativo/politicas/{politica}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Mecanico_nao_passa_na_politica_de_atendente()
    {
        var resposta = await _factory.CriarClienteComPapel("mecanico").GetAsync("/api/administrativo/politicas/atendente");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Saude_confirma_que_o_banco_responde_apos_as_migrations()
    {
        var resposta = await _factory.CreateClient().GetAsync("/saude");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public void Servicos_transversais_estao_registrados()
    {
        using var escopo = _factory.Services.CreateScope();

        Assert.NotNull(escopo.ServiceProvider.GetRequiredService<TimeProvider>());
        Assert.NotNull(escopo.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>());
        Assert.NotNull(escopo.ServiceProvider.GetRequiredService<IDespachanteDeEventos>());
    }

    [Fact]
    public async Task Documento_openapi_declara_o_esquema_oauth2()
    {
        var resposta = await _factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("oauth2", await resposta.Content.ReadAsStringAsync());
    }
}
