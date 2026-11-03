using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Catalogo;
using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class ListagemDeOsTests : IAsyncLifetime
{
    private const string Rota = "/api/administrativo/ordens-de-servico";

    private readonly string _conexao;
    private readonly ApiFactory _origem;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly DateTimeOffset _referencia = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private ServicoDto _servico = null!;

    public ListagemDeOsTests(PostgresFixture postgres)
    {
        _conexao = postgres.ConnectionString($"oficina_listagem_{Guid.NewGuid():N}");
        _origem = new ApiFactory(postgres);
        _factory = _origem.WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Oficina", _conexao));
    }

    public async Task InitializeAsync()
    {
        var resposta = await Como("gerente")
            .PostAsJsonAsync("/api/administrativo/servicos", new { nome = "Troca de oleo", descricao = (string?)null, preco = 150m });
        resposta.EnsureSuccessStatusCode();
        _servico = (await resposta.Content.ReadFromJsonAsync<ServicoDto>())!;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _origem.DisposeAsync();
    }

    private HttpClient Como(string papel)
    {
        var http = _factory.CreateClient();
        http.DefaultRequestHeaders.Add(ApiFactory.CabecalhoPapel, papel);
        return http;
    }

    private async Task<Guid> OsCom(StatusOs status, int horasAposAReferencia)
    {
        var http = Como("atendente");
        var cliente = (await (await http.PostAsJsonAsync("/api/administrativo/clientes",
                new { nome = "Maria Silva", documento = CpfNovo(), email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null }))
            .Content.ReadFromJsonAsync<ClienteDto>())!;
        var veiculo = (await (await http.PostAsJsonAsync("/api/administrativo/veiculos",
                new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id }))
            .Content.ReadFromJsonAsync<VeiculoDto>())!;
        var abertura = await http.PostAsJsonAsync(Rota, new { clienteId = cliente.Id, veiculoId = veiculo.Id, servicoIds = new[] { _servico.Id } });
        Assert.Equal(HttpStatusCode.Created, abertura.StatusCode);
        var os = (await abertura.Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;

        await ColocarNoStatus(os.Id, status, _referencia.AddHours(horasAposAReferencia));
        return os.Id;
    }

    private async Task ColocarNoStatus(Guid osId, StatusOs status, DateTimeOffset abertaEm)
    {
        await using var conexao = new NpgsqlConnection(_conexao);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(
            "UPDATE ordens_de_servico SET status = @status, \"AbertaEm\" = @abertaEm WHERE \"Id\" = @id", conexao);
        comando.Parameters.AddWithValue("status", status.ToString());
        comando.Parameters.AddWithValue("abertaEm", abertaEm);
        comando.Parameters.AddWithValue("id", osId);
        Assert.Equal(1, await comando.ExecuteNonQueryAsync());
    }

    private async Task<PaginaDeOrdensDeServicoDto> Listar(string consulta = "")
    {
        var resposta = await Como("atendente").GetAsync($"{Rota}{consulta}");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PaginaDeOrdensDeServicoDto>())!;
    }

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
        var resto = digitos.Select((d, i) => d * (pesoInicial - i)).Sum() % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    [Fact]
    public async Task Sem_filtro_mostra_so_as_os_que_exigem_trabalho_ate_a_finalizacao()
    {
        var recebida = await OsCom(StatusOs.Recebida, 0);
        var emExecucao = await OsCom(StatusOs.EmExecucao, 1);
        await OsCom(StatusOs.Finalizada, 2);
        await OsCom(StatusOs.Entregue, 3);
        await OsCom(StatusOs.Cancelada, 4);

        var pagina = await Listar();

        Assert.Equal([emExecucao, recebida], pagina.Itens.Select(o => o.Id));
        Assert.Equal((1, 20, 2), (pagina.Pagina, pagina.Tamanho, pagina.Total));
    }

    [Fact]
    public async Task Ordena_pela_prioridade_do_status_e_depois_pelas_mais_antigas()
    {
        var recebida = await OsCom(StatusOs.Recebida, -10);
        var emDiagnostico = await OsCom(StatusOs.EmDiagnostico, -9);
        var aguardando = await OsCom(StatusOs.AguardandoAprovacao, -8);
        var aprovada = await OsCom(StatusOs.Aprovada, -7);
        var emExecucaoRecente = await OsCom(StatusOs.EmExecucao, -1);
        var emExecucaoAntiga = await OsCom(StatusOs.EmExecucao, -6);

        var pagina = await Listar();

        Assert.Equal(
            [emExecucaoAntiga, emExecucaoRecente, aprovada, aguardando, emDiagnostico, recebida],
            pagina.Itens.Select(o => o.Id));
        Assert.Equal(
            ["EmExecucao", "EmExecucao", "Aprovada", "AguardandoAprovacao", "EmDiagnostico", "Recebida"],
            pagina.Itens.Select(o => o.Status));
    }

    [Fact]
    public async Task Filtro_explicito_por_finalizada_mostra_as_os_prontas_para_entrega()
    {
        await OsCom(StatusOs.Recebida, 0);
        var finalizadaRecente = await OsCom(StatusOs.Finalizada, 2);
        var finalizadaAntiga = await OsCom(StatusOs.Finalizada, 1);
        await OsCom(StatusOs.Entregue, 3);

        var pagina = await Listar("?status=Finalizada");

        Assert.Equal([finalizadaAntiga, finalizadaRecente], pagina.Itens.Select(o => o.Id));
        Assert.Equal(2, pagina.Total);
    }

    [Theory]
    [InlineData("Entregue")]
    [InlineData("Cancelada")]
    public async Task Filtro_explicito_mostra_exatamente_o_status_pedido(string status)
    {
        await OsCom(StatusOs.Recebida, 0);
        var pedida = await OsCom(Enum.Parse<StatusOs>(status), 1);

        var pagina = await Listar($"?status={status}");

        Assert.Equal([pedida], pagina.Itens.Select(o => o.Id));
    }

    [Fact]
    public async Task Pagina_o_resultado_e_informa_o_total_do_filtro()
    {
        var abertas = new List<Guid>();
        for (var hora = 0; hora < 5; hora++)
        {
            abertas.Add(await OsCom(StatusOs.Recebida, hora));
        }
        await OsCom(StatusOs.Finalizada, 9);

        var primeira = await Listar("?pagina=1&tamanho=2");
        var segunda = await Listar("?pagina=2&tamanho=2");
        var terceira = await Listar("?pagina=3&tamanho=2");
        var alemDoFim = await Listar("?pagina=4&tamanho=2");

        Assert.Equal(abertas[..2], primeira.Itens.Select(o => o.Id));
        Assert.Equal(abertas[2..4], segunda.Itens.Select(o => o.Id));
        Assert.Equal(abertas[4..], terceira.Itens.Select(o => o.Id));
        Assert.Empty(alemDoFim.Itens);
        Assert.Equal((2, 2, 5), (segunda.Pagina, segunda.Tamanho, segunda.Total));
        Assert.Equal(5, alemDoFim.Total);
    }

    [Fact]
    public async Task Os_abertas_no_mesmo_momento_tem_ordem_estavel_entre_paginas()
    {
        var empatadas = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            empatadas.Add(await OsCom(StatusOs.Aprovada, 0));
        }

        var paginas = new List<Guid>();
        for (var pagina = 1; pagina <= 4; pagina++)
        {
            paginas.AddRange((await Listar($"?pagina={pagina}&tamanho=1")).Itens.Select(o => o.Id));
        }

        Assert.Equal(empatadas.Order(), paginas);
    }

    [Theory]
    [InlineData("?pagina=0")]
    [InlineData("?pagina=-1")]
    [InlineData("?tamanho=0")]
    [InlineData("?tamanho=101")]
    public async Task Rejeita_pagina_ou_tamanho_fora_dos_limites(string consulta)
    {
        var resposta = await Como("atendente").GetAsync($"{Rota}{consulta}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Theory]
    [InlineData("?pagina=abc")]
    [InlineData("?tamanho=1.5")]
    public async Task Rejeita_pagina_ou_tamanho_nao_numerico_no_binding(string consulta)
    {
        var resposta = await Como("atendente").GetAsync($"{Rota}{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Aceita_os_limites_de_pagina_e_tamanho()
    {
        await OsCom(StatusOs.Recebida, 0);

        var minimo = await Listar("?pagina=1&tamanho=1");
        var maximo = await Listar("?tamanho=100");

        Assert.Single(minimo.Itens);
        Assert.Equal(100, maximo.Tamanho);
    }

    [Fact]
    public async Task Mecanico_tambem_lista_com_e_sem_filtro_de_status()
    {
        var recebida = await OsCom(StatusOs.Recebida, 0);
        var mecanico = Como("mecanico");

        var padrao = await mecanico.GetFromJsonAsync<PaginaDeOrdensDeServicoDto>(Rota);
        var recebidas = await mecanico.GetFromJsonAsync<PaginaDeOrdensDeServicoDto>($"{Rota}?status=Recebida");
        var entregues = await mecanico.GetFromJsonAsync<PaginaDeOrdensDeServicoDto>($"{Rota}?status=Entregue");

        Assert.Equal([recebida], padrao!.Itens.Select(o => o.Id));
        Assert.Equal([recebida], recebidas!.Itens.Select(o => o.Id));
        Assert.Empty(entregues!.Itens);
    }

    [Fact]
    public async Task Fila_de_execucao_traz_so_as_aprovadas_pela_abertura()
    {
        var aprovadaRecente = await OsCom(StatusOs.Aprovada, 2);
        var aprovadaAntiga = await OsCom(StatusOs.Aprovada, 1);
        await OsCom(StatusOs.EmExecucao, 0);

        var fila = await Como("mecanico").GetFromJsonAsync<List<OrdemDeServicoResumoDto>>($"{Rota}/fila-de-execucao");

        Assert.Equal([aprovadaAntiga, aprovadaRecente], fila!.Select(o => o.Id));
    }
}
