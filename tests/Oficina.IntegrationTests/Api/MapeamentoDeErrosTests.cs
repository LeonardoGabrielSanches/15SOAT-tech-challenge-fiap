using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Api.Erros;
using Oficina.Domain.Common;

namespace Oficina.IntegrationTests.Api;

public class MapeamentoDeErrosTests
{
    [Theory]
    [InlineData(typeof(RecursoNaoEncontradoException), 404)]
    [InlineData(typeof(ConflitoDeEstadoException), 409)]
    [InlineData(typeof(DadosInvalidosException), 422)]
    public async Task Excecao_de_dominio_vira_problem_details_com_o_status_esperado(Type tipo, int status)
    {
        var contexto = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddOptions().AddProblemDetails().BuildServiceProvider()
        };
        contexto.Response.Body = new MemoryStream();
        var tratador = new TratadorDeExcecoesDeDominio(contexto.RequestServices.GetRequiredService<IProblemDetailsService>());
        var excecao = (Exception)Activator.CreateInstance(tipo, "falhou")!;

        var tratada = await tratador.TryHandleAsync(contexto, excecao, CancellationToken.None);

        Assert.True(tratada);
        Assert.Equal(status, contexto.Response.StatusCode);
        contexto.Response.Body.Position = 0;
        var problema = await System.Text.Json.JsonSerializer.DeserializeAsync<ProblemDetails>(
            contexto.Response.Body,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal(status, problema!.Status);
        Assert.Equal("falhou", problema.Detail);
    }

    [Fact]
    public async Task Excecao_desconhecida_nao_e_tratada()
    {
        var contexto = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddOptions().AddProblemDetails().BuildServiceProvider()
        };
        var tratador = new TratadorDeExcecoesDeDominio(contexto.RequestServices.GetRequiredService<IProblemDetailsService>());

        var tratada = await tratador.TryHandleAsync(contexto, new InvalidOperationException(), CancellationToken.None);

        Assert.False(tratada);
    }

    [Fact]
    public async Task Violacao_de_unicidade_do_banco_vira_409()
    {
        var contexto = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddOptions().AddProblemDetails().BuildServiceProvider()
        };
        contexto.Response.Body = new MemoryStream();
        var tratador = new TratadorDeExcecoesDeDominio(contexto.RequestServices.GetRequiredService<IProblemDetailsService>());
        var excecao = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "falhou",
            new Npgsql.PostgresException("duplicado", "ERROR", "ERROR", Npgsql.PostgresErrorCodes.UniqueViolation));

        var tratada = await tratador.TryHandleAsync(contexto, excecao, CancellationToken.None);

        Assert.True(tratada);
        Assert.Equal(409, contexto.Response.StatusCode);
    }
}
