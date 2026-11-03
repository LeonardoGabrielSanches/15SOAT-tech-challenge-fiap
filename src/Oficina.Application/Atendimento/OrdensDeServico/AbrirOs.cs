using Oficina.Application.Atendimento.Clientes;
using Oficina.Application.Atendimento.Veiculos;
using Oficina.Application.Common;
using Oficina.Domain.Atendimento.Entities;
using Oficina.Domain.Atendimento.Interfaces;
using Oficina.Domain.CatalogoEstoque.Entities;
using Oficina.Domain.CatalogoEstoque.Interfaces;
using Oficina.Domain.Common;

namespace Oficina.Application.Atendimento.OrdensDeServico;

public sealed record AbrirOsComando(Guid ClienteId, Guid VeiculoId, IReadOnlyCollection<Guid> ServicoIds, string Autor);

public sealed class AbrirOs(
    IOrdemDeServicoRepository ordens,
    IClienteRepository clientes,
    IVeiculoRepository veiculos,
    IServicoRepository servicos,
    IGeradorNumeroOs gerador,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<OrdemDeServicoResumoDto> Executar(AbrirOsComando comando, CancellationToken cancellationToken)
    {
        var cliente = await BuscaDeCliente.ObterOuFalhar(clientes, comando.ClienteId, cancellationToken);
        var veiculo = await BuscaDeVeiculo.ObterOuFalhar(veiculos, comando.VeiculoId, cancellationToken);
        var servicosSolicitados = await ServicosSolicitados(comando.ServicoIds, cancellationToken);

        OrdemDeServico.GarantirAberturaPermitida(cliente, veiculo, servicosSolicitados);

        if (await ordens.ExisteEmAndamentoDoVeiculo(veiculo.Id, cancellationToken))
        {
            throw new ConflitoDeEstadoException("O Veiculo ja possui uma OS em andamento.");
        }

        var agora = relogio.GetUtcNow();
        var numero = await gerador.Proximo(agora.Year, cancellationToken);
        var os = OrdemDeServico.Abrir(numero, cliente, veiculo, servicosSolicitados, comando.Autor, agora);

        ordens.Adicionar(os);
        await unidadeDeTrabalho.Confirmar(cancellationToken);
        return OrdemDeServicoResumoDto.De(os);
    }

    private async Task<IReadOnlyList<Servico>> ServicosSolicitados(IReadOnlyCollection<Guid> servicoIds, CancellationToken cancellationToken)
    {
        var encontrados = (await servicos.ObterPorIds(servicoIds.Distinct().ToList(), cancellationToken)).ToDictionary(s => s.Id);

        return servicoIds
            .Select(id => encontrados.TryGetValue(id, out var servico)
                ? servico
                : throw new RecursoNaoEncontradoException("Servico nao encontrado."))
            .ToList();
    }
}
