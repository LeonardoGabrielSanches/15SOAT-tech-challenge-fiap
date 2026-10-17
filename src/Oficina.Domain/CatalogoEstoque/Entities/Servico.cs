using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.Domain.CatalogoEstoque.Entities;

public sealed class Servico : RaizDeAgregado
{
    private Servico()
    {
        Nome = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public Dinheiro Preco { get; private set; }
    public bool Ativo { get; private set; }

    public static Servico Cadastrar(string nome, string? descricao, Dinheiro preco)
    {
        var servico = new Servico { Id = Guid.NewGuid(), Ativo = true };
        servico.Atualizar(nome, descricao, preco);
        return servico;
    }

    public void Atualizar(string nome, string? descricao, Dinheiro preco)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DadosInvalidosException("O nome do servico e obrigatorio.");
        }

        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        Preco = preco;
    }

    public void Inativar() => Ativo = false;

    public void GarantirAtivo()
    {
        if (!Ativo)
        {
            throw new ConflitoDeEstadoException("Servico inativo nao pode ser usado em novas operacoes.");
        }
    }
}
