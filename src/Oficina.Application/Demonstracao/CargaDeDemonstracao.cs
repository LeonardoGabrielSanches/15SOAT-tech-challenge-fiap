using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Entities.ValueObjects;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Entities.ValueObjects;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Estoque.Entities;
using Oficina.Domain.Estoque.Entities.Enums;
using Oficina.Domain.Estoque.Entities.ValueObjects;
using Oficina.Domain.Estoque.Interfaces;

namespace Oficina.Application.Demonstracao;

public sealed class CargaDeDemonstracao(
    IServicoRepository servicos,
    IPecaInsumoRepository itens,
    IClienteRepository clientes,
    IVeiculoRepository veiculos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    private const string CpfDoCliente = "529.982.247-25";
    private const string PlacaDoVeiculo = "BRA2E19";

    public async Task Executar(CancellationToken cancellationToken)
    {
        await CarregarServicos(cancellationToken);
        await CarregarItens(cancellationToken);
        await CarregarClienteComVeiculo(cancellationToken);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
    }

    private async Task CarregarServicos(CancellationToken cancellationToken)
    {
        var existentes = (await servicos.Listar(cancellationToken)).Select(s => s.Nome).ToHashSet();
        (string Nome, string Descricao, decimal Preco)[] catalogo =
        [
            ("Troca de óleo", "Troca do óleo do motor e do filtro", 120m),
            ("Alinhamento e balanceamento", "Alinhamento de direção e balanceamento das rodas", 150m),
            ("Revisão de freios", "Inspeção e ajuste do sistema de freios", 200m)
        ];

        foreach (var (nome, descricao, preco) in catalogo.Where(c => !existentes.Contains(c.Nome)))
        {
            servicos.Adicionar(Servico.Cadastrar(nome, descricao, Dinheiro.De(preco)));
        }
    }

    private async Task CarregarItens(CancellationToken cancellationToken)
    {
        var existentes = (await itens.Listar(true, cancellationToken)).Select(i => i.Nome).ToHashSet();
        (string Nome, TipoPecaInsumo Tipo, string Unidade, decimal Preco, decimal Saldo)[] catalogo =
        [
            ("Filtro de óleo", TipoPecaInsumo.Peca, "un", 35m, 20m),
            ("Óleo 5W30", TipoPecaInsumo.Insumo, "L", 45m, 50.5m)
        ];

        foreach (var (nome, tipo, unidade, preco, saldo) in catalogo.Where(c => !existentes.Contains(c.Nome)))
        {
            var item = PecaInsumo.Criar(nome, tipo, unidade, Dinheiro.De(preco));
            item.RegistrarEntrada(Quantidade.Criar(saldo, tipo));
            itens.Adicionar(item);
        }
    }

    private async Task CarregarClienteComVeiculo(CancellationToken cancellationToken)
    {
        var documento = Documento.Criar(CpfDoCliente);
        var cliente = await clientes.ObterPorDocumento(documento, cancellationToken);
        if (cliente is null)
        {
            cliente = Cliente.Cadastrar("Maria Silva", documento, Email.Criar("maria.silva@exemplo.com"), "11999990000");
            clientes.Adicionar(cliente);
        }

        var placa = Placa.Criar(PlacaDoVeiculo);
        if (await veiculos.ObterPorPlaca(placa, cancellationToken) is null)
        {
            veiculos.Adicionar(Veiculo.Cadastrar(placa, "Fiat", "Argo", AnoFabricacao.Criar(2021, relogio), cliente));
        }
    }
}
