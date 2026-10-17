using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;

namespace Oficina.Domain.Atendimento.Interfaces;

public interface IVeiculoRepository
{
    Task<Veiculo?> ObterPorId(Guid id, CancellationToken cancellationToken);

    Task<Veiculo?> ObterPorPlaca(Placa placa, CancellationToken cancellationToken);

    Task<IReadOnlyList<Veiculo>> ListarPorProprietario(Guid proprietarioId, bool? ativo, CancellationToken cancellationToken);

    void Adicionar(Veiculo veiculo);
}
