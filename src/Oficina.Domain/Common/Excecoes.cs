namespace Oficina.Domain.Common;

public abstract class ExcecaoDeDominio(string mensagem) : Exception(mensagem);

public sealed class RecursoNaoEncontradoException(string mensagem) : ExcecaoDeDominio(mensagem);

public sealed class ConflitoDeEstadoException(string mensagem) : ExcecaoDeDominio(mensagem);

public sealed class DadosInvalidosException(string mensagem) : ExcecaoDeDominio(mensagem);

public sealed class AcessoNegadoException(string mensagem) : ExcecaoDeDominio(mensagem);
