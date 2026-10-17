using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.Common;

namespace Oficina.Domain.Atendimento.Entities;

public sealed class ServicoDaOs
{
    private ServicoDaOs()
    {
    }

    internal ServicoDaOs(Guid servicoId) => ServicoId = servicoId;

    public Guid ServicoId { get; private set; }
}

public sealed class EntradaDoHistoricoDeStatus
{
    private EntradaDoHistoricoDeStatus()
    {
        Autor = null!;
    }

    internal EntradaDoHistoricoDeStatus(StatusOs? origem, StatusOs destino, DateTimeOffset momento, string autor)
    {
        Origem = origem;
        Destino = destino;
        Momento = momento;
        Autor = autor;
    }

    public StatusOs? Origem { get; private set; }

    public StatusOs Destino { get; private set; }

    public DateTimeOffset Momento { get; private set; }

    public string Autor { get; private set; }
}

public sealed class OrdemDeServico : RaizDeAgregado
{
    private readonly List<ServicoDaOs> _servicos = [];
    private readonly List<EntradaDoHistoricoDeStatus> _historico = [];

    private OrdemDeServico()
    {
        Numero = null!;
    }

    public Guid Id { get; private set; }

    public NumeroOs Numero { get; private set; }

    public Guid ClienteId { get; private set; }

    public Guid VeiculoId { get; private set; }

    public StatusOs Status { get; private set; }

    public DateTimeOffset AbertaEm { get; private set; }

    public IReadOnlyList<ServicoDaOs> Servicos => _servicos;

    public IReadOnlyList<EntradaDoHistoricoDeStatus> Historico => _historico;

    public static OrdemDeServico Abrir(
        NumeroOs numero,
        Cliente cliente,
        Veiculo veiculo,
        IReadOnlyCollection<Servico> servicos,
        string autor,
        DateTimeOffset agora)
    {
        cliente.GarantirAtivo();
        veiculo.GarantirAtivo();

        if (veiculo.ProprietarioId != cliente.Id)
        {
            throw new DadosInvalidosException("O Cliente informado nao e o Proprietario atual do Veiculo.");
        }

        if (servicos.Count == 0)
        {
            throw new DadosInvalidosException("A OS deve ter ao menos um Servico.");
        }

        if (servicos.Select(s => s.Id).Distinct().Count() != servicos.Count)
        {
            throw new DadosInvalidosException("Servico repetido na OS.");
        }

        foreach (var servico in servicos)
        {
            servico.GarantirAtivo();
        }

        var os = new OrdemDeServico
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            ClienteId = cliente.Id,
            VeiculoId = veiculo.Id,
            Status = StatusOs.Recebida,
            AbertaEm = agora
        };
        os._servicos.AddRange(servicos.Select(s => new ServicoDaOs(s.Id)));
        os._historico.Add(new EntradaDoHistoricoDeStatus(null, StatusOs.Recebida, agora, autor));
        os.Registrar(new OsAberta(os.Id, numero.Valor, cliente.Id, veiculo.Id, agora));
        return os;
    }

    public void IncluirServico(Servico servico)
    {
        GarantirRecebida();
        servico.GarantirAtivo();

        if (_servicos.Any(s => s.ServicoId == servico.Id))
        {
            throw new ConflitoDeEstadoException("O Servico ja esta na OS.");
        }

        _servicos.Add(new ServicoDaOs(servico.Id));
    }

    public void RemoverServico(Guid servicoId)
    {
        GarantirRecebida();

        var existente = _servicos.FirstOrDefault(s => s.ServicoId == servicoId)
            ?? throw new RecursoNaoEncontradoException("O Servico nao esta na OS.");

        if (_servicos.Count == 1)
        {
            throw new ConflitoDeEstadoException("A OS deve manter ao menos um Servico.");
        }

        _servicos.Remove(existente);
    }

    private void GarantirRecebida()
    {
        if (Status != StatusOs.Recebida)
        {
            throw new ConflitoDeEstadoException("Os servicos so podem ser ajustados pelo Atendente enquanto a OS esta Recebida.");
        }
    }
}
