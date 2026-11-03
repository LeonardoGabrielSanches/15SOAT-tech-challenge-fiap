using System.Net;
using System.Net.Http.Json;
using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.OrdensDeServico;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Catalogo;
using Oficina.IntegrationTests.Support;

namespace Oficina.IntegrationTests.Api.Atendimento;

[Collection(PostgresCollection.Nome)]
public class ConsultaPublicaDaOsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string RotaPublica = "/api/publico/ordens-de-servico/consulta";
    private const string RotaAdministrativa = "/api/administrativo/ordens-de-servico";

    private readonly ApiFactory _factory = new(postgres);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

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

    private static string PlacaNova() =>
        new string(Enumerable.Range(0, 3).Select(_ => (char)('A' + Random.Shared.Next(26))).ToArray()) + Random.Shared.Next(1000, 10000);

    private async Task<(string Numero, string Cpf)> AbrirOs()
    {
        var gerente = _factory.CriarClienteComPapel("gerente");
        var servicoResposta = await gerente.PostAsJsonAsync("/api/administrativo/servicos",
            new { nome = "Troca de oleo", descricao = (string?)null, preco = 150m });
        servicoResposta.EnsureSuccessStatusCode();
        var servico = (await servicoResposta.Content.ReadFromJsonAsync<ServicoDto>())!;

        var atendente = _factory.CriarClienteComPapel("atendente");
        var cpf = CpfNovo();
        var clienteResposta = await atendente.PostAsJsonAsync("/api/administrativo/clientes",
            new { nome = "Maria Silva", documento = cpf, email = $"{Guid.NewGuid():N}@oficina.com", telefone = (string?)null });
        clienteResposta.EnsureSuccessStatusCode();
        var cliente = (await clienteResposta.Content.ReadFromJsonAsync<ClienteDto>())!;

        var veiculoResposta = await atendente.PostAsJsonAsync("/api/administrativo/veiculos",
            new { placa = PlacaNova(), marca = "Fiat", modelo = "Uno", anoFabricacao = 2015, proprietarioId = cliente.Id });
        veiculoResposta.EnsureSuccessStatusCode();
        var veiculo = (await veiculoResposta.Content.ReadFromJsonAsync<VeiculoDto>())!;

        var osResposta = await atendente.PostAsJsonAsync(RotaAdministrativa,
            new { clienteId = cliente.Id, veiculoId = veiculo.Id, servicoIds = new[] { servico.Id } });
        osResposta.EnsureSuccessStatusCode();
        var os = (await osResposta.Content.ReadFromJsonAsync<OrdemDeServicoResumoDto>())!;
        return (os.Numero, cpf);
    }

    [Fact]
    public async Task Consulta_valida_sem_token_retorna_status_e_historico_sem_nome_de_funcionario()
    {
        var (numero, cpf) = await AbrirOs();

        var resposta = await _factory.CreateClient().PostAsJsonAsync(RotaPublica, new { numero, documento = cpf });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("usuario-de-teste", corpo);
        var consulta = (await resposta.Content.ReadFromJsonAsync<ConsultaPublicaDaOsDto>())!;
        Assert.Equal(numero, consulta.Numero);
        Assert.Equal("Recebida", consulta.Status);
        var entrada = Assert.Single(consulta.Historico);
        Assert.Equal("Oficina", entrada.Autor);
        Assert.Null(consulta.OrcamentoVigente);
    }

    [Fact]
    public async Task Documento_formatado_tambem_e_aceito()
    {
        var (numero, cpf) = await AbrirOs();
        var formatado = $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";

        var resposta = await _factory.CreateClient().PostAsJsonAsync(RotaPublica, new { numero, documento = formatado });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Documento_divergente_e_os_inexistente_retornam_a_mesma_resposta_404()
    {
        var (numero, _) = await AbrirOs();
        var http = _factory.CreateClient();

        var divergente = await http.PostAsJsonAsync(RotaPublica, new { numero, documento = CpfNovo() });
        var inexistente = await http.PostAsJsonAsync(RotaPublica, new { numero = "OS-2026-99999", documento = CpfNovo() });

        Assert.Equal(HttpStatusCode.NotFound, divergente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        var problemaDivergente = (await divergente.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!;
        var problemaInexistente = (await inexistente.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!;
        Assert.Equal((problemaInexistente.Status, problemaInexistente.Title, problemaInexistente.Detail),
            (problemaDivergente.Status, problemaDivergente.Title, problemaDivergente.Detail));
    }
}
