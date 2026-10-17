using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Oficina.Domain.Common;

namespace Oficina.Api.Erros;

public sealed class TratadorDeExcecoesDeDominio(IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception excecao, CancellationToken cancellationToken)
    {
        var (status, titulo) = excecao switch
        {
            RecursoNaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso nao encontrado"),
            ConflitoDeEstadoException => (StatusCodes.Status409Conflict, "Conflito de estado"),
            DadosInvalidosException => (StatusCodes.Status422UnprocessableEntity, "Dados invalidos"),
            _ => (0, "")
        };

        if (status == 0)
        {
            return false;
        }

        http.Response.StatusCode = status;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = excecao,
            ProblemDetails = new ProblemDetails { Status = status, Title = titulo, Detail = excecao.Message }
        });
    }
}
