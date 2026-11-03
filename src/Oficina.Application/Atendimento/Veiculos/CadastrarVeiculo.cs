using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.Veiculos;

public sealed record CadastrarVeiculoComando(string? Placa, string? Marca, string? Modelo, int AnoFabricacao, Guid ProprietarioId);

public sealed class CadastrarVeiculo(
    IVeiculoRepository veiculos,
    IClienteRepository clientes,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<VeiculoDto> Executar(CadastrarVeiculoComando comando, CancellationToken cancellationToken)
    {
        var placa = Placa.Criar(comando.Placa);
        var ano = AnoFabricacao.Criar(comando.AnoFabricacao, relogio);
        var proprietario = await BuscaDeCliente.ObterOuFalhar(clientes, comando.ProprietarioId, cancellationToken);
        var veiculo = Veiculo.Cadastrar(placa, comando.Marca, comando.Modelo, ano, proprietario);

        if (await veiculos.ObterPorPlaca(placa, cancellationToken) is not null)
        {
            throw new ConflitoDeEstadoException("Ja existe um Veiculo cadastrado com esta placa.");
        }

        veiculos.Adicionar(veiculo);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return VeiculoDto.De(veiculo);
    }
}
