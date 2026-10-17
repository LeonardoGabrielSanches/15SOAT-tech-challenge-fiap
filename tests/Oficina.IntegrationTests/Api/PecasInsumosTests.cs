using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Common;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api;

[Collection(PostgresCollection.Nome)]
public class PecasInsumosTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Gerente() => _factory.CriarClienteComPapel("gerente");

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<JsonElement>());

    private async Task<Guid> Cadastrar(string tipo, string nome, string unidade, decimal preco)
    {
        var resposta = await Gerente().PostAsJsonAsync("/api/pecas-insumos", new { nome, tipo, unidadeDeMedida = unidade, preco });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await Ler(resposta)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Sem_token_responde_401()
    {
        var resposta = await _factory.CreateClient().GetAsync("/api/pecas-insumos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Mecanico_nao_cadastra()
    {
        var resposta = await _factory.CriarClienteComPapel("mecanico")
            .PostAsJsonAsync("/api/pecas-insumos", new { nome = "x", tipo = "Peca", unidadeDeMedida = "un", preco = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastra_consulta_atualiza_e_inativa()
    {
        var id = await Cadastrar("Peca", "Filtro de óleo", "un", 25m);

        var obtido = await Ler(await Gerente().GetAsync($"/api/pecas-insumos/{id}"));
        Assert.Equal("Filtro de óleo", obtido.GetProperty("nome").GetString());
        Assert.Equal(25m, obtido.GetProperty("preco").GetDecimal());
        Assert.Equal(0m, obtido.GetProperty("saldo").GetDecimal());

        var atualizado = await Gerente().PutAsJsonAsync($"/api/pecas-insumos/{id}",
            new { nome = "Filtro de óleo premium", unidadeDeMedida = "un", preco = 30.5m });
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);
        Assert.Equal(30.5m, (await Ler(atualizado)).GetProperty("preco").GetDecimal());

        var excluido = await Gerente().DeleteAsync($"/api/pecas-insumos/{id}");
        Assert.Equal(HttpStatusCode.NoContent, excluido.StatusCode);

        var depois = await Ler(await Gerente().GetAsync($"/api/pecas-insumos/{id}"));
        Assert.False(depois.GetProperty("ativo").GetBoolean());
        var ativos = await Ler(await Gerente().GetAsync("/api/pecas-insumos"));
        Assert.DoesNotContain(ativos.EnumerateArray(), e => e.GetProperty("id").GetGuid() == id);
        var todos = await Ler(await Gerente().GetAsync("/api/pecas-insumos?incluirInativos=true"));
        Assert.Contains(todos.EnumerateArray(), e => e.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Entrada_aumenta_o_saldo_consultavel()
    {
        var id = await Cadastrar("Insumo", "Óleo 5W30", "L", 42.9m);

        var entrada = await Gerente().PostAsJsonAsync($"/api/pecas-insumos/{id}/entradas", new { quantidade = 4.5m });
        Assert.Equal(HttpStatusCode.OK, entrada.StatusCode);
        await Gerente().PostAsJsonAsync($"/api/pecas-insumos/{id}/entradas", new { quantidade = 1m });

        var saldo = await Ler(await Gerente().GetAsync($"/api/pecas-insumos/{id}/saldo"));
        Assert.Equal(5.5m, saldo.GetProperty("saldo").GetDecimal());
    }

    [Fact]
    public async Task Entrada_fracionada_de_peca_responde_422()
    {
        var id = await Cadastrar("Peca", "Vela", "un", 10m);

        var resposta = await Gerente().PostAsJsonAsync($"/api/pecas-insumos/{id}/entradas", new { quantidade = 1.5m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Item_inexistente_responde_404()
    {
        var resposta = await Gerente().GetAsync($"/api/pecas-insumos/{Guid.NewGuid()}/saldo");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Entrada_em_item_inativo_responde_409()
    {
        var id = await Cadastrar("Peca", "Correia", "un", 80m);
        await Gerente().DeleteAsync($"/api/pecas-insumos/{id}");

        var resposta = await Gerente().PostAsJsonAsync($"/api/pecas-insumos/{id}/entradas", new { quantidade = 1 });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Gravacoes_concorrentes_nao_se_sobrescrevem()
    {
        var id = await Cadastrar("Peca", "Pastilha", "jogo", 120m);

        using var escopoA = _factory.Services.CreateScope();
        using var escopoB = _factory.Services.CreateScope();
        var repositorioA = escopoA.ServiceProvider.GetRequiredService<IRepositorioDePecaInsumo>();
        var repositorioB = escopoB.ServiceProvider.GetRequiredService<IRepositorioDePecaInsumo>();
        var itemA = await repositorioA.Obter(id, default);
        var itemB = await repositorioB.Obter(id, default);

        itemA!.RegistrarEntrada(Quantidade.Criar(3m, TipoPecaInsumo.Peca));
        itemB!.RegistrarEntrada(Quantidade.Criar(5m, TipoPecaInsumo.Peca));
        await escopoA.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>().Confirmar(default);

        await Assert.ThrowsAsync<ConflitoDeEstadoException>(() =>
            escopoB.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>().Confirmar(default));

        var saldo = await Ler(await Gerente().GetAsync($"/api/pecas-insumos/{id}/saldo"));
        Assert.Equal(3m, saldo.GetProperty("saldo").GetDecimal());
    }
}
