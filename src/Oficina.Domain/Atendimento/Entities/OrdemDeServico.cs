using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque;

namespace Oficina.Domain.Atendimento.Entities;

public sealed class ServicoDaOs
{
    private ServicoDaOs()
    {
    }

    internal ServicoDaOs(Guid servicoId) => ServicoId = servicoId;

    public Guid ServicoId { get; private set; }
}

public sealed class ItemDaOs
{
    private ItemDaOs()
    {
    }

    internal ItemDaOs(Guid pecaInsumoId, decimal quantidade)
    {
        PecaInsumoId = pecaInsumoId;
        Quantidade = quantidade;
    }

    public Guid PecaInsumoId { get; private set; }

    public decimal Quantidade { get; internal set; }
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
    private readonly List<ItemDaOs> _itensDePecaInsumo = [];
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

    public IReadOnlyList<ItemDaOs> ItensDePecaInsumo => _itensDePecaInsumo;

    public string? MotivoDaUltimaRecusa { get; private set; }

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
        GarantirServicosEditaveis();
        servico.GarantirAtivo();

        if (_servicos.Any(s => s.ServicoId == servico.Id))
        {
            throw new ConflitoDeEstadoException("O Servico ja esta na OS.");
        }

        _servicos.Add(new ServicoDaOs(servico.Id));
    }

    public void RemoverServico(Guid servicoId)
    {
        GarantirServicosEditaveis();

        var existente = _servicos.FirstOrDefault(s => s.ServicoId == servicoId)
            ?? throw new RecursoNaoEncontradoException("O Servico nao esta na OS.");

        if (_servicos.Count == 1)
        {
            throw new ConflitoDeEstadoException("A OS deve manter ao menos um Servico.");
        }

        _servicos.Remove(existente);
    }

    public void IniciarDiagnostico(string autor, DateTimeOffset agora)
    {
        if (Status != StatusOs.Recebida)
        {
            throw new ConflitoDeEstadoException("O diagnostico so pode ser iniciado em uma OS Recebida.");
        }

        MudarStatus(StatusOs.EmDiagnostico, autor, agora);
        Registrar(new DiagnosticoIniciado(Id, Numero.Valor, agora));
    }

    public void IncluirPecaInsumo(PecaInsumo peca, Quantidade quantidade, DateTimeOffset agora)
    {
        GarantirEmDiagnostico();

        peca.Baixar(quantidade);

        var existente = _itensDePecaInsumo.FirstOrDefault(i => i.PecaInsumoId == peca.Id);
        if (existente is null)
        {
            _itensDePecaInsumo.Add(new ItemDaOs(peca.Id, quantidade.Valor));
        }
        else
        {
            existente.Quantidade += quantidade.Valor;
        }

        Registrar(new ItemIncluido(Id, peca.Id, quantidade.Valor, agora));
    }

    public void RemoverPecaInsumo(PecaInsumo peca, decimal? quantidade)
    {
        GarantirEmDiagnostico();

        var existente = _itensDePecaInsumo.FirstOrDefault(i => i.PecaInsumoId == peca.Id)
            ?? throw new RecursoNaoEncontradoException("A Peca/insumo nao esta na OS.");

        var removida = quantidade ?? existente.Quantidade;
        if (removida > existente.Quantidade)
        {
            throw new DadosInvalidosException("A quantidade a remover excede a quantidade incluida na OS.");
        }

        peca.Estornar(Quantidade.Criar(removida, peca.Tipo));

        if (removida == existente.Quantidade)
        {
            _itensDePecaInsumo.Remove(existente);
        }
        else
        {
            existente.Quantidade -= removida;
        }
    }

    private void MudarStatus(StatusOs destino, string autor, DateTimeOffset agora)
    {
        _historico.Add(new EntradaDoHistoricoDeStatus(Status, destino, agora, autor));
        Status = destino;
    }

    private void GarantirEmDiagnostico()
    {
        if (Status != StatusOs.EmDiagnostico)
        {
            throw new ConflitoDeEstadoException("Pecas e insumos so podem ser ajustados enquanto a OS esta Em diagnostico.");
        }
    }

    private void GarantirServicosEditaveis()
    {
        if (Status is not (StatusOs.Recebida or StatusOs.EmDiagnostico))
        {
            throw new ConflitoDeEstadoException("Os servicos so podem ser ajustados enquanto a OS esta Recebida ou Em diagnostico.");
        }
    }
}
