using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.ValueObjects;

namespace Oficina.Domain.Atendimento.Entities;

public sealed record InclusaoDePecaInsumo(PecaInsumo Peca, Quantidade Quantidade);
