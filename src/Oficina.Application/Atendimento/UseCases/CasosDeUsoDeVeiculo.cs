using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.UseCases;

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

internal static class BuscaDeCliente
{
    public static async Task<Cliente> ObterOuFalhar(IClienteRepository clientes, Guid id, CancellationToken cancellationToken) =>
        await clientes.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Cliente nao encontrado.");
}

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

public sealed class ConsultarVeiculo(IVeiculoRepository veiculos)
{
    public async Task<VeiculoDto> Executar(Guid id, CancellationToken cancellationToken)
    {
        var veiculo = await veiculos.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Veiculo nao encontrado.");
        return VeiculoDto.De(veiculo);
    }
}

public sealed class ListarVeiculosDoCliente(IVeiculoRepository veiculos, IClienteRepository clientes)
{
    public async Task<IReadOnlyList<VeiculoDto>> Executar(Guid clienteId, bool? ativo, CancellationToken cancellationToken)
    {
        await BuscaDeCliente.ObterOuFalhar(clientes, clienteId, cancellationToken);
        var encontrados = await veiculos.ListarPorProprietario(clienteId, ativo, cancellationToken);
        return encontrados.Select(VeiculoDto.De).ToList();
    }
}

public sealed record AtualizarVeiculoComando(Guid Id, string? Marca, string? Modelo, int AnoFabricacao);

public sealed class AtualizarVeiculo(IVeiculoRepository veiculos, IUnidadeDeTrabalho unidadeDeTrabalho, TimeProvider relogio)
{
    public async Task<VeiculoDto> Executar(AtualizarVeiculoComando comando, CancellationToken cancellationToken)
    {
        var veiculo = await veiculos.ObterPorId(comando.Id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Veiculo nao encontrado.");

        veiculo.AtualizarDados(comando.Marca, comando.Modelo, AnoFabricacao.Criar(comando.AnoFabricacao, relogio));

        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return VeiculoDto.De(veiculo);
    }
}

public sealed class InativarVeiculo(IVeiculoRepository veiculos, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Executar(Guid id, CancellationToken cancellationToken)
    {
        var veiculo = await veiculos.ObterPorId(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Veiculo nao encontrado.");

        veiculo.Inativar();

        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }
}

public sealed record TransferirVeiculoComando(Guid VeiculoId, Guid NovoProprietarioId);

public sealed class TransfereVeiculo(IVeiculoRepository veiculos, IClienteRepository clientes, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task<VeiculoDto> Executar(TransferirVeiculoComando comando, CancellationToken cancellationToken)
    {
        var veiculo = await veiculos.ObterPorId(comando.VeiculoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Veiculo nao encontrado.");
        var novoProprietario = await BuscaDeCliente.ObterOuFalhar(clientes, comando.NovoProprietarioId, cancellationToken);

        veiculo.Transferir(novoProprietario);

        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return VeiculoDto.De(veiculo);
    }
}
