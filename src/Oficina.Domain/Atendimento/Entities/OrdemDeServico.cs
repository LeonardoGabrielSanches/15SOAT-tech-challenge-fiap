using Oficina.Domain.Atendimento.Entities.Enums;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Events;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.Common;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.ValueObjects;

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
    private readonly List<VersaoDoOrcamento> _versoes = [];

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

    public IReadOnlyList<VersaoDoOrcamento> Versoes => _versoes;

    public VersaoDoOrcamento? VersaoVigente => _versoes.Count == 0 ? null : _versoes[^1];

    public string? MotivoDaUltimaRecusa { get; private set; }

    public IReadOnlyList<EntradaDoHistoricoDeStatus> Historico => _historico;

    public static OrdemDeServico Abrir(
        NumeroOs numero,
        Cliente cliente,
        Veiculo veiculo,
        IReadOnlyCollection<Servico> servicos,
        string autor,
        DateTimeOffset agora,
        IReadOnlyCollection<PecaInsumoSolicitada>? pecasInsumos = null)
    {
        GarantirAberturaPermitida(cliente, veiculo, servicos, pecasInsumos);

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
        foreach (var solicitada in pecasInsumos ?? [])
        {
            os.AdicionarPecaInsumo(solicitada.Peca, solicitada.Quantidade, agora);
        }

        return os;
    }

    public static void GarantirAberturaPermitida(
        Cliente cliente,
        Veiculo veiculo,
        IReadOnlyCollection<Servico> servicos,
        IReadOnlyCollection<PecaInsumoSolicitada>? pecasInsumos = null)
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

        if (pecasInsumos is not null && pecasInsumos.Select(p => p.Peca.Id).Distinct().Count() != pecasInsumos.Count)
        {
            throw new DadosInvalidosException("Peca/insumo repetida na OS.");
        }
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
        AdicionarPecaInsumo(peca, quantidade, agora);
    }

    private void AdicionarPecaInsumo(PecaInsumo peca, Quantidade quantidade, DateTimeOffset agora)
    {
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

    public void ConcluirDiagnostico(
        IReadOnlyCollection<Servico> servicos,
        IReadOnlyCollection<PecaInsumo> pecas,
        string autor,
        DateTimeOffset agora)
    {
        if (Status != StatusOs.EmDiagnostico)
        {
            throw new ConflitoDeEstadoException("O diagnostico so pode ser concluido em uma OS Em diagnostico.");
        }

        if (_servicos.Count == 0)
        {
            throw new DadosInvalidosException("A OS deve ter ao menos um Servico para concluir o diagnostico.");
        }

        var itens = new List<ItemDaVersao>();
        foreach (var vinculo in _servicos)
        {
            var servico = servicos.FirstOrDefault(s => s.Id == vinculo.ServicoId)
                ?? throw new RecursoNaoEncontradoException("Servico nao encontrado.");
            itens.Add(new ItemDaVersao(TipoDoItemDoOrcamento.Servico, servico.Id, servico.Nome, 1m, servico.Preco.Valor));
        }

        foreach (var item in _itensDePecaInsumo)
        {
            var peca = pecas.FirstOrDefault(p => p.Id == item.PecaInsumoId)
                ?? throw new RecursoNaoEncontradoException("Peca/insumo nao encontrada.");
            itens.Add(new ItemDaVersao(TipoDoItemDoOrcamento.PecaInsumo, peca.Id, peca.Nome, item.Quantidade, peca.Preco.Valor));
        }

        foreach (var anterior in _versoes)
        {
            anterior.Superar();
        }

        var versao = new VersaoDoOrcamento(_versoes.Count + 1, itens, agora);
        _versoes.Add(versao);

        MudarStatus(StatusOs.AguardandoAprovacao, autor, agora);
        Registrar(new DiagnosticoConcluido(Id, Numero.Valor, ClienteId, versao.Numero, versao.Total, versao.Token, agora));
    }

    public void AprovarOrcamento(string token, string autor, DateTimeOffset agora)
    {
        var versao = ObterVersaoPendenteDoToken(token);

        versao.Aprovar();
        MudarStatus(StatusOs.Aprovada, autor, agora);
        Registrar(new OrcamentoAprovado(Id, Numero.Valor, versao.Numero, agora));
    }

    public void RecusarOrcamento(string token, string? motivo, string autor, DateTimeOffset agora)
    {
        var motivoNormalizado = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        if (motivoNormalizado is { Length: > 500 })
        {
            throw new DadosInvalidosException("O motivo da recusa deve ter no maximo 500 caracteres.");
        }

        var versao = ObterVersaoPendenteDoToken(token);

        versao.Recusar(motivoNormalizado);
        MotivoDaUltimaRecusa = motivoNormalizado;
        MudarStatus(StatusOs.EmDiagnostico, autor, agora);
        Registrar(new OrcamentoRecusado(Id, Numero.Valor, versao.Numero, motivoNormalizado, agora));
    }

    public void IniciarExecucao(string autor, DateTimeOffset agora)
    {
        Transitar(StatusOs.Aprovada, StatusOs.EmExecucao, "A execucao so pode ser iniciada em uma OS Aprovada.", autor, agora);
        Registrar(new ExecucaoIniciada(Id, Numero.Valor, agora));
    }

    public void FinalizarExecucao(string autor, DateTimeOffset agora)
    {
        Transitar(StatusOs.EmExecucao, StatusOs.Finalizada, "A execucao so pode ser finalizada em uma OS Em execucao.", autor, agora);
        Registrar(new ExecucaoFinalizada(Id, Numero.Valor, agora));
    }

    public void RegistrarEntrega(string autor, DateTimeOffset agora)
    {
        Transitar(StatusOs.Finalizada, StatusOs.Entregue, "A entrega so pode ser registrada em uma OS Finalizada.", autor, agora);
        Registrar(new EntregaRealizada(Id, Numero.Valor, agora));
    }

    private void Transitar(StatusOs esperado, StatusOs destino, string mensagem, string autor, DateTimeOffset agora)
    {
        if (Status != esperado)
        {
            throw new ConflitoDeEstadoException(mensagem);
        }

        MudarStatus(destino, autor, agora);
    }

    private VersaoDoOrcamento ObterVersaoPendenteDoToken(string token)
    {
        var versao = _versoes.FirstOrDefault(v => v.Token == token)
            ?? throw new RecursoNaoEncontradoException("Token de aprovacao nao encontrado.");

        if (versao.Situacao != SituacaoDaVersao.Pendente)
        {
            throw new ConflitoDeEstadoException("O token de aprovacao ja foi usado ou pertence a uma versao superada.");
        }

        if (Status != StatusOs.AguardandoAprovacao)
        {
            throw new ConflitoDeEstadoException("A OS nao esta Aguardando aprovacao.");
        }

        return versao;
    }

    public void Cancelar(string motivo, IReadOnlyCollection<PecaInsumo> pecas, string autor, DateTimeOffset agora)
    {
        if (Status is not (StatusOs.Recebida or StatusOs.EmDiagnostico or StatusOs.AguardandoAprovacao))
        {
            throw new ConflitoDeEstadoException("A OS so pode ser cancelada enquanto estiver Recebida, Em diagnostico ou Aguardando aprovacao.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new DadosInvalidosException("O motivo do cancelamento e obrigatorio.");
        }

        foreach (var item in _itensDePecaInsumo)
        {
            var peca = pecas.FirstOrDefault(p => p.Id == item.PecaInsumoId)
                ?? throw new RecursoNaoEncontradoException("Peca/insumo nao encontrada.");
            peca.Estornar(Quantidade.Criar(item.Quantidade, peca.Tipo));
        }

        _itensDePecaInsumo.Clear();

        foreach (var versao in _versoes)
        {
            versao.Superar();
        }

        MudarStatus(StatusOs.Cancelada, autor, agora);
        Registrar(new OsCancelada(Id, Numero.Valor, motivo.Trim(), agora));
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
