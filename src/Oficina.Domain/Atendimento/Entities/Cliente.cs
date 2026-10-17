using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities;

public sealed class Cliente : RaizDeAgregado
{
    private const int TamanhoMaximoDoNome = 200;
    private const int TamanhoMaximoDoTelefone = 20;

    private Cliente()
    {
        Nome = null!;
        Documento = null!;
        Email = null!;
    }

    public Guid Id { get; private set; }

    public string Nome { get; private set; }

    public Documento Documento { get; private set; }

    public Email Email { get; private set; }

    public string? Telefone { get; private set; }

    public bool Ativo { get; private set; }

    public static Cliente Cadastrar(string? nome, Documento documento, Email email, string? telefone)
    {
        return new Cliente
        {
            Id = Guid.NewGuid(),
            Nome = NormalizarNome(nome),
            Documento = documento,
            Email = email,
            Telefone = NormalizarTelefone(telefone),
            Ativo = true
        };
    }

    public void AtualizarDados(string? nome, Email email, string? telefone)
    {
        GarantirAtivo();
        Nome = NormalizarNome(nome);
        Email = email;
        Telefone = NormalizarTelefone(telefone);
    }

    public void Inativar() => Ativo = false;

    public void GarantirAtivo()
    {
        if (!Ativo)
        {
            throw new ConflitoDeEstadoException("Cliente inativo nao pode ser usado em novas operacoes.");
        }
    }

    private static string NormalizarNome(string? nome)
    {
        var normalizado = (nome ?? "").Trim();
        if (normalizado.Length == 0 || normalizado.Length > TamanhoMaximoDoNome)
        {
            throw new DadosInvalidosException($"Nome e obrigatorio e deve ter ate {TamanhoMaximoDoNome} caracteres.");
        }

        return normalizado;
    }

    private static string? NormalizarTelefone(string? telefone)
    {
        var normalizado = telefone?.Trim();
        if (string.IsNullOrEmpty(normalizado))
        {
            return null;
        }

        if (normalizado.Length > TamanhoMaximoDoTelefone)
        {
            throw new DadosInvalidosException($"Telefone deve ter ate {TamanhoMaximoDoTelefone} caracteres.");
        }

        return normalizado;
    }
}
