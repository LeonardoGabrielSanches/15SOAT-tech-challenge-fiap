using Oficina.Domain.Atendimento.Entities;

namespace Oficina.Application.Atendimento.Veiculos;

public sealed record VeiculoDto(
    Guid Id,
    string Placa,
    string Marca,
    string Modelo,
    int AnoFabricacao,
    Guid ProprietarioId,
    bool Ativo)
{
    public static VeiculoDto De(Veiculo veiculo) => new(
        veiculo.Id,
        veiculo.Placa.Valor,
        veiculo.Marca,
        veiculo.Modelo,
        veiculo.AnoFabricacao.Valor,
        veiculo.ProprietarioId,
        veiculo.Ativo);
}
