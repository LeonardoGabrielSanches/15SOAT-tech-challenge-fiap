using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities.ValueObjects;

public sealed record Documento
{
    private Documento(TipoDocumento tipo, string numero)
    {
        Tipo = tipo;
        Numero = numero;
    }

    public TipoDocumento Tipo { get; }

    public string Numero { get; }

    public static Documento Criar(string? valor)
    {
        var tamanho = (valor ?? "").Count(c => !char.IsWhiteSpace(c) && c is not '.' and not '/' and not '-');

        return tamanho switch
        {
            11 => new Documento(TipoDocumento.Cpf, Cpf.Criar(valor).Numero),
            14 => new Documento(TipoDocumento.Cnpj, Cnpj.Criar(valor).Numero),
            _ => throw new DadosInvalidosException("Documento deve ser um CPF ou um CNPJ valido.")
        };
    }
}
