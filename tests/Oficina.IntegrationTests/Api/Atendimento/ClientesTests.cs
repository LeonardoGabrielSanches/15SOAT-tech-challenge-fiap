using System.Net;
using System.Net.Http.Json;
using Oficina.Application.Atendimento.Clientes;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class ClientesTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/clientes";

    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Atendente() => _factory.CriarClienteComPapel("atendente");

    private static object Corpo(string documento, string email = "maria@oficina.com", string? telefone = "11999990000") =>
        new { nome = "Maria Silva", documento, email, telefone };

    [Fact]
    public async Task Cadastra_e_consulta_cliente_com_cpf_mascarado()
    {
        var http = Atendente();

        var criacao = await http.PostAsJsonAsync(Rota, Corpo("529.982.247-25"));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var criado = (await criacao.Content.ReadFromJsonAsync<ClienteDto>())!;
        Assert.Equal("52998224725", criado.Documento);
        Assert.True(criado.Ativo);
        Assert.EndsWith($"{Rota}/{criado.Id}", criacao.Headers.Location!.ToString());

        var consulta = await http.GetFromJsonAsync<ClienteDto>($"{Rota}/{criado.Id}");
        Assert.Equal(criado, consulta);
    }

    [Fact]
    public async Task Cadastra_cliente_com_cnpj_alfanumerico()
    {
        var resposta = await Atendente().PostAsJsonAsync(Rota, Corpo("12.ABC.345/01DE-35", "contato@empresa.com", null));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = (await resposta.Content.ReadFromJsonAsync<ClienteDto>())!;
        Assert.Equal("Cnpj", criado.TipoDocumento);
        Assert.Equal("12ABC34501DE35", criado.Documento);
        Assert.Null(criado.Telefone);
    }

    [Fact]
    public async Task Documento_duplicado_responde_409_mesmo_com_mascara_diferente()
    {
        var http = Atendente();
        await http.PostAsJsonAsync(Rota, Corpo("111.444.777-35"));

        var resposta = await http.PostAsJsonAsync(Rota, Corpo("11144477735", "outra@oficina.com"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData("529.982.247-24", "maria@oficina.com")]
    [InlineData("11.222.333/0001-80", "maria@oficina.com")]
    [InlineData("123", "maria@oficina.com")]
    [InlineData("000.000.001-91", "invalido")]
    public async Task Dados_invalidos_respondem_422(string documento, string email)
    {
        var resposta = await Atendente().PostAsJsonAsync(Rota, Corpo(documento, email));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualiza_dados_de_contato()
    {
        var http = Atendente();
        var criado = (await (await http.PostAsJsonAsync(Rota, Corpo("11.444.777/0001-61"))).Content.ReadFromJsonAsync<ClienteDto>())!;

        var resposta = await http.PutAsJsonAsync($"{Rota}/{criado.Id}", new { nome = "Empresa Nova", email = "novo@empresa.com", telefone = (string?)null });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var atualizado = (await resposta.Content.ReadFromJsonAsync<ClienteDto>())!;
        Assert.Equal("Empresa Nova", atualizado.Nome);
        Assert.Equal("novo@empresa.com", atualizado.Email);
        Assert.Null(atualizado.Telefone);
        Assert.Equal(criado.Documento, atualizado.Documento);
    }

    [Fact]
    public async Task Delete_inativa_sem_excluir_e_cliente_continua_consultavel()
    {
        var http = Atendente();
        var criado = (await (await http.PostAsJsonAsync(Rota, Corpo("11.222.333/0001-81"))).Content.ReadFromJsonAsync<ClienteDto>())!;

        var delete = await http.DeleteAsync($"{Rota}/{criado.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var consulta = await http.GetFromJsonAsync<ClienteDto>($"{Rota}/{criado.Id}");
        Assert.False(consulta!.Ativo);

        var atualizacao = await http.PutAsJsonAsync($"{Rota}/{criado.Id}", new { nome = "X", email = "x@x.com", telefone = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, atualizacao.StatusCode);
    }

    [Fact]
    public async Task Lista_filtra_por_situacao_e_por_documento()
    {
        var http = Atendente();
        var ativo = (await (await http.PostAsJsonAsync(Rota, Corpo("000.000.001-91"))).Content.ReadFromJsonAsync<ClienteDto>())!;

        var porDocumento = await http.GetFromJsonAsync<List<ClienteDto>>($"{Rota}?documento=000.000.001-91");
        Assert.Equal(ativo.Id, Assert.Single(porDocumento!).Id);

        await http.DeleteAsync($"{Rota}/{ativo.Id}");
        var inativos = await http.GetFromJsonAsync<List<ClienteDto>>($"{Rota}?ativo=false");
        var ativos = await http.GetFromJsonAsync<List<ClienteDto>>($"{Rota}?ativo=true");
        Assert.Contains(inativos!, c => c.Id == ativo.Id);
        Assert.DoesNotContain(ativos!, c => c.Id == ativo.Id);

        var invalido = await http.GetAsync($"{Rota}?documento=123");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalido.StatusCode);
    }

    [Fact]
    public async Task Cliente_inexistente_responde_404()
    {
        var http = Atendente();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"{Rota}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.DeleteAsync($"{Rota}/{id}")).StatusCode);
    }

    [Fact]
    public async Task Sem_token_responde_401()
    {
        var resposta = await _factory.CreateClient().GetAsync(Rota);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Mecanico_responde_403()
    {
        var resposta = await _factory.CriarClienteComPapel("mecanico").GetAsync(Rota);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Gerente_tambem_acessa()
    {
        var resposta = await _factory.CriarClienteComPapel("gerente").GetAsync(Rota);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
