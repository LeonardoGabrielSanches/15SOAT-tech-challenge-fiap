using System.Net;
using System.Net.Http.Json;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api;

[Collection(PostgresCollection.Nome)]
public class ServicosTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/servicos";
    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private sealed record ServicoResposta(Guid Id, string Nome, string? Descricao, decimal Preco, bool Ativo);

    private async Task<ServicoResposta> Cadastrar(string nome = "Troca de oleo", decimal preco = 120m)
    {
        var resposta = await _factory.CriarClienteComPapel("gerente")
            .PostAsJsonAsync(Rota, new { nome, descricao = "Completa", preco });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<ServicoResposta>())!;
    }

    [Fact]
    public async Task Gerente_cadastra_consulta_atualiza_e_inativa()
    {
        var gerente = _factory.CriarClienteComPapel("gerente");
        var criado = await Cadastrar("Alinhamento CRUD", 80m);
        Assert.True(criado.Ativo);

        var consulta = await gerente.GetFromJsonAsync<ServicoResposta>($"{Rota}/{criado.Id}");
        Assert.Equal("Alinhamento CRUD", consulta!.Nome);
        Assert.Equal(80m, consulta.Preco);

        var atualizacao = await gerente.PutAsJsonAsync($"{Rota}/{criado.Id}",
            new { nome = "Alinhamento 3D", descricao = (string?)null, preco = 95.5m });
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
        var atualizado = await atualizacao.Content.ReadFromJsonAsync<ServicoResposta>();
        Assert.Equal("Alinhamento 3D", atualizado!.Nome);
        Assert.Equal(95.5m, atualizado.Preco);

        var exclusao = await gerente.DeleteAsync($"{Rota}/{criado.Id}");
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);

        var aposInativar = await gerente.GetFromJsonAsync<ServicoResposta>($"{Rota}/{criado.Id}");
        Assert.False(aposInativar!.Ativo);

        var lista = await gerente.GetFromJsonAsync<List<ServicoResposta>>(Rota);
        Assert.Contains(lista!, s => s.Id == criado.Id && !s.Ativo);
    }

    [Theory]
    [InlineData("atendente")]
    [InlineData("mecanico")]
    [InlineData("gerente")]
    public async Task Leitura_e_liberada_para_os_tres_papeis(string papel)
    {
        var criado = await Cadastrar("Leitura " + papel);
        var cliente = _factory.CriarClienteComPapel(papel);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(Rota)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"{Rota}/{criado.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("atendente")]
    [InlineData("mecanico")]
    public async Task Escrita_com_papel_errado_responde_403(string papel)
    {
        var criado = await Cadastrar("Escrita " + papel);
        var cliente = _factory.CriarClienteComPapel(papel);
        var corpo = new { nome = "X", descricao = (string?)null, preco = 1m };

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(Rota, corpo)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsJsonAsync($"{Rota}/{criado.Id}", corpo)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.DeleteAsync($"{Rota}/{criado.Id}")).StatusCode);
    }

    [Fact]
    public async Task Sem_token_responde_401()
    {
        var cliente = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync(Rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await cliente.PostAsJsonAsync(Rota, new { nome = "X", preco = 1m })).StatusCode);
    }

    [Fact]
    public async Task Preco_negativo_responde_422()
    {
        var resposta = await _factory.CriarClienteComPapel("gerente")
            .PostAsJsonAsync(Rota, new { nome = "Invalido", preco = -1m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Servico_inexistente_responde_404()
    {
        var resposta = await _factory.CriarClienteComPapel("gerente").GetAsync($"{Rota}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
