import SwiftUI

struct OrdemDeServicoView: View {
    @Environment(Sessao.self) private var sessao

    let ordem: OrdemDeServicoResumo

    @State private var detalhe: OrdemDeServicoDetalhe?
    @State private var erro: String?
    @State private var naoEncontrada = false

    var body: some View {
        Group {
            if let detalhe {
                conteudo(detalhe)
            } else if naoEncontrada {
                ContentUnavailableView("OS não encontrada", systemImage: "questionmark.folder", description: Text("Esta OS não existe mais."))
            } else if let erro {
                ContentUnavailableView("Não foi possível carregar", systemImage: "exclamationmark.triangle", description: Text(erro))
            } else {
                ProgressView()
            }
        }
        .navigationTitle(ordem.numero)
        .navigationBarTitleDisplayMode(.inline)
        .refreshable { await carregar() }
        .task { await carregar() }
    }

    private func conteudo(_ os: OrdemDeServicoDetalhe) -> some View {
        List {
            AcoesDeDiagnosticoView(os: os, recarregar: carregar)
            Section("OS") {
                LabeledContent("Número da OS", value: os.numero)
                LabeledContent("Status", value: os.rotuloDoStatus)
                LabeledContent("Aberta em", value: DataDaApi.formatar(os.abertaEm))
            }
            Section("Cliente") {
                Text(os.cliente.nome)
            }
            Section("Veículo") {
                LabeledContent("Placa", value: os.veiculo.placa)
                LabeledContent("Marca", value: os.veiculo.marca)
                LabeledContent("Modelo", value: os.veiculo.modelo)
                LabeledContent("Ano", value: String(os.veiculo.anoFabricacao))
            }
            Section("Serviços") {
                if os.servicos.isEmpty {
                    Text("Nenhum Serviço na OS.").foregroundStyle(.secondary)
                }
                ForEach(os.servicos) { servico in
                    LabeledContent(servico.nome, value: Formatacao.moeda(servico.preco))
                }
            }
            Section("Peças/insumos") {
                if os.pecasInsumos.isEmpty {
                    Text("Nenhuma Peça/insumo na OS.").foregroundStyle(.secondary)
                }
                ForEach(os.pecasInsumos) { item in
                    VStack(alignment: .leading, spacing: 2) {
                        Text(item.nome)
                        Text("\(Formatacao.quantidade(item.quantidade)) \(item.unidadeDeMedida) · \(Formatacao.moeda(item.preco)) cada")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
            if let motivo = os.motivoDaUltimaRecusa {
                Section("Motivo da última recusa") {
                    Text(motivo)
                }
            }
            Section("Versões do orçamento") {
                if os.versoesDoOrcamento.isEmpty {
                    Text("Nenhuma Versão do orçamento.").foregroundStyle(.secondary)
                }
                ForEach(os.versoesDoOrcamento) { versao in
                    VersaoDoOrcamentoView(versao: versao)
                }
            }
            Section("Histórico de status") {
                ForEach(os.historico) { entrada in
                    VStack(alignment: .leading, spacing: 2) {
                        Text("\(entrada.rotuloDaOrigem) → \(entrada.rotuloDoDestino)")
                        Text("\(DataDaApi.formatar(entrada.momento)) · \(entrada.autor)")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
        }
    }

    private func carregar() async {
        do {
            detalhe = try await ClienteApi(sessao: sessao).detalharOrdemDeServico(id: ordem.id)
            erro = nil
            naoEncontrada = false
        } catch ErroApi.naoEncontrado {
            if !Task.isCancelled {
                detalhe = nil
                naoEncontrada = true
            }
        } catch {
            if !Task.isCancelled { erro = error.localizedDescription }
        }
    }
}

private struct VersaoDoOrcamentoView: View {
    let versao: VersaoDoOrcamento

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack {
                Text("Versão \(versao.numero)").font(.headline)
                Spacer()
                Text(versao.situacao).font(.subheadline)
            }
            ForEach(versao.itens) { item in
                HStack {
                    Text("\(Formatacao.quantidade(item.quantidade)) × \(item.descricao)")
                    Spacer()
                    Text(Formatacao.moeda(item.subtotal))
                }
                .font(.caption)
            }
            LabeledContent("Total", value: Formatacao.moeda(versao.total)).font(.subheadline.bold())
            if let motivo = versao.motivoDaRecusa {
                Text("Recusa: \(motivo)").font(.caption).foregroundStyle(.secondary)
            }
        }
    }
}

enum Formatacao {
    static func moeda(_ valor: Decimal) -> String {
        valor.formatted(.currency(code: "BRL").locale(Locale(identifier: "pt_BR")))
    }

    static func quantidade(_ valor: Decimal) -> String {
        valor.formatted(.number.locale(Locale(identifier: "pt_BR")))
    }
}
