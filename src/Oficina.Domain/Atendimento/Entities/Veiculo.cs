using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities;

public sealed class Veiculo : RaizDeAgregado
{
    private const int TamanhoMaximoDeMarcaEModelo = 100;

    private Veiculo()
    {
        Placa = null!;
        Marca = null!;
        Modelo = null!;
        AnoFabricacao = null!;
    }

    public Guid Id { get; private set; }

    public Placa Placa { get; private set; }

    public string Marca { get; private set; }

    public string Modelo { get; private set; }

    public AnoFabricacao AnoFabricacao { get; private set; }

    public Guid ProprietarioId { get; private set; }

    public bool Ativo { get; private set; }

    public static Veiculo Cadastrar(Placa placa, string? marca, string? modelo, AnoFabricacao anoFabricacao, Cliente proprietario)
    {
        proprietario.GarantirAtivo();
        return new Veiculo
        {
            Id = Guid.NewGuid(),
            Placa = placa,
            Marca = Normalizar(marca, "Marca"),
            Modelo = Normalizar(modelo, "Modelo"),
            AnoFabricacao = anoFabricacao,
            ProprietarioId = proprietario.Id,
            Ativo = true
        };
    }

    public void AtualizarDados(string? marca, string? modelo, AnoFabricacao anoFabricacao)
    {
        GarantirAtivo();
        Marca = Normalizar(marca, "Marca");
        Modelo = Normalizar(modelo, "Modelo");
        AnoFabricacao = anoFabricacao;
    }

    public void Transferir(Cliente novoProprietario)
    {
        GarantirAtivo();
        novoProprietario.GarantirAtivo();
        ProprietarioId = novoProprietario.Id;
    }

    public void Inativar() => Ativo = false;

    public void GarantirAtivo()
    {
        if (!Ativo)
        {
            throw new ConflitoDeEstadoException("Veiculo inativo nao pode ser usado em novas operacoes.");
        }
    }

    private static string Normalizar(string? valor, string campo)
    {
        var normalizado = (valor ?? "").Trim();
        if (normalizado.Length == 0 || normalizado.Length > TamanhoMaximoDeMarcaEModelo)
        {
            throw new DadosInvalidosException($"{campo} e obrigatorio e deve ter ate {TamanhoMaximoDeMarcaEModelo} caracteres.");
        }

        return normalizado;
    }
}
