using System.Net;
using System.Net.Http.Json;
using Oficina.Application.Atendimento.UseCases;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class VeiculosTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string RotaClientes = "/api/administrativo/clientes";
    private const string Rota = "/api/administrativo/veiculos";

    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HttpClient Atendente() => _factory.CriarClienteComPapel("atendente");

    private static async Task<ClienteDto> CriarCliente(HttpClient http, string documento, string email)
    {
        var resposta = await http.PostAsJsonAsync(RotaClientes, new { nome = "Maria Silva", documento, email, telefone = (string?)null });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ClienteDto>())!;
    }


    private static string Email() => $"{Guid.NewGuid():N}@oficina.com";

    private static string PlacaNova() =>
        new string(Enumerable.Range(0, 3).Select(_ => (char)('A' + Random.Shared.Next(26))).ToArray()) + Random.Shared.Next(1000, 10000);

    private static string CpfNovo()
    {
        var digitos = Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(10)).ToList();
        if (digitos.Distinct().Count() == 1) digitos[0] = (digitos[0] + 1) % 10;
        digitos.Add(Digito(digitos, 10));
        digitos.Add(Digito(digitos, 11));
        return string.Concat(digitos);
    }

    private static int Digito(List<int> digitos, int pesoInicial)
    {
        var soma = digitos.Select((d, i) => d * (pesoInicial - i)).Sum();
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static object Corpo(string placa, Guid proprietarioId, int ano = 2015) =>
        new { placa, marca = "Fiat", modelo = "Uno", anoFabricacao = ano, proprietarioId };

    [Fact]
    public async Task Cadastra_consulta_atualiza_lista_e_inativa()
    {
        var http = Atendente();
        var cliente = await CriarCliente(http, CpfNovo(), Email());

        var placa = PlacaNova();

        var criacao = await http.PostAsJsonAsync(Rota, Corpo(placa.ToLowerInvariant().Insert(3, "-"), cliente.Id));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var criado = (await criacao.Content.ReadFromJsonAsync<VeiculoDto>())!;
        Assert.Equal(placa, criado.Placa);
        Assert.Equal(cliente.Id, criado.ProprietarioId);
        Assert.EndsWith($"{Rota}/{criado.Id}", criacao.Headers.Location!.ToString());
        Assert.Equal(criado, await http.GetFromJsonAsync<VeiculoDto>($"{Rota}/{criado.Id}"));

        var atualizacao = await http.PutAsJsonAsync($"{Rota}/{criado.Id}", new { marca = "VW", modelo = "Gol", anoFabricacao = 2020 });
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);
        var atualizado = (await atualizacao.Content.ReadFromJsonAsync<VeiculoDto>())!;
        Assert.Equal(("VW", "Gol", 2020), (atualizado.Marca, atualizado.Modelo, atualizado.AnoFabricacao));

        var lista = await http.GetFromJsonAsync<List<VeiculoDto>>($"{RotaClientes}/{cliente.Id}/veiculos");
        Assert.Equal([criado.Id], lista!.Select(v => v.Id));

        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"{Rota}/{criado.Id}")).StatusCode);
        Assert.False((await http.GetFromJsonAsync<VeiculoDto>($"{Rota}/{criado.Id}"))!.Ativo);
    }

    [Fact]
    public async Task Placa_duplicada_responde_409_mesmo_com_formatacao_diferente()
    {
        var http = Atendente();
        var cliente = await CriarCliente(http, CpfNovo(), Email());
        var placa = PlacaNova();
        await http.PostAsJsonAsync(Rota, Corpo(placa, cliente.Id));

        var resposta = await http.PostAsJsonAsync(Rota, Corpo(placa.ToLowerInvariant().Insert(3, "-"), cliente.Id));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData("ABC123", 2015)]
    [InlineData("ABC1234", 1899)]
    public async Task Dados_invalidos_respondem_422(string placa, int ano)
    {
        var http = Atendente();
        var cliente = await CriarCliente(http, CpfNovo(), Email());

        var resposta = await http.PostAsJsonAsync(Rota, Corpo(placa, cliente.Id, ano));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastro_com_cliente_inexistente_responde_404()
    {
        var resposta = await Atendente().PostAsJsonAsync(Rota, Corpo(PlacaNova(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastro_com_cliente_inativo_responde_409()
    {
        var http = Atendente();
        var cliente = await CriarCliente(http, CpfNovo(), Email());
        await http.DeleteAsync($"{RotaClientes}/{cliente.Id}");

        var resposta = await http.PostAsJsonAsync(Rota, Corpo(PlacaNova(), cliente.Id));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Transfere_veiculo_para_outro_cliente_e_atualiza_as_listagens()
    {
        var http = Atendente();
        var antigo = await CriarCliente(http, CpfNovo(), Email());
        var novo = await CriarCliente(http, CpfNovo(), Email());
        var veiculo = (await (await http.PostAsJsonAsync(Rota, Corpo(PlacaNova(), antigo.Id))).Content.ReadFromJsonAsync<VeiculoDto>())!;

        var transferencia = await http.PostAsJsonAsync($"{Rota}/{veiculo.Id}/transferencia", new { novoProprietarioId = novo.Id });

        Assert.Equal(HttpStatusCode.OK, transferencia.StatusCode);
        Assert.Equal(novo.Id, (await transferencia.Content.ReadFromJsonAsync<VeiculoDto>())!.ProprietarioId);
        Assert.Empty((await http.GetFromJsonAsync<List<VeiculoDto>>($"{RotaClientes}/{antigo.Id}/veiculos"))!);
        Assert.Single((await http.GetFromJsonAsync<List<VeiculoDto>>($"{RotaClientes}/{novo.Id}/veiculos"))!);
    }

    [Fact]
    public async Task Transferencia_para_cliente_inexistente_responde_404_e_para_inativo_409()
    {
        var http = Atendente();
        var antigo = await CriarCliente(http, CpfNovo(), Email());
        var inativo = await CriarCliente(http, CpfNovo(), Email());
        await http.DeleteAsync($"{RotaClientes}/{inativo.Id}");
        var veiculo = (await (await http.PostAsJsonAsync(Rota, Corpo(PlacaNova(), antigo.Id))).Content.ReadFromJsonAsync<VeiculoDto>())!;

        var inexistente = await http.PostAsJsonAsync($"{Rota}/{veiculo.Id}/transferencia", new { novoProprietarioId = Guid.NewGuid() });
        var paraInativo = await http.PostAsJsonAsync($"{Rota}/{veiculo.Id}/transferencia", new { novoProprietarioId = inativo.Id });

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, paraInativo.StatusCode);
    }

    [Fact]
    public async Task Exige_papel_de_atendente()
    {
        var resposta = await _factory.CriarClienteComPapel("mecanico").GetAsync($"{Rota}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
