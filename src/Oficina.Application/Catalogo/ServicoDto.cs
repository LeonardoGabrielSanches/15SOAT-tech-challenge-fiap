using Oficina.Domain.CatalogoEstoque.Entities;

namespace Oficina.Application.Catalogo;

public sealed record ServicoDto(Guid Id, string Nome, string? Descricao, decimal Preco, bool Ativo)
{
    public static ServicoDto De(Servico servico) =>
        new(servico.Id, servico.Nome, servico.Descricao, servico.Preco.Valor, servico.Ativo);
}
