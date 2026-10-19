import SwiftUI

struct AcoesDeDiagnosticoView: View {
    @Environment(Sessao.self) private var sessao

    let os: OrdemDeServicoDetalhe
    let recarregar: () async -> Void

    @State private var executando = false
    @State private var mensagemDeErro: String?
    @State private var incluindoServico = false
    @State private var incluindoPecaInsumo = false
    @State private var pecaInsumoARemover: PecaInsumoDaOs?
    @State private var servicoARemover: ServicoDaOs?
    @State private var confirmandoConclusao = false

    private var capacidades: CapacidadesDaOs { CapacidadesDaOs(status: os.statusDaOs) }

    var body: some View {
        Group {
            if capacidades.permite(.iniciarDiagnostico) || capacidades.permite(.concluirDiagnostico) {
                Section("Diagnóstico") {
                    if capacidades.permite(.iniciarDiagnostico) {
                        Button("Iniciar diagnóstico") {
                            Task { _ = await executar { try await ClienteApi(sessao: sessao).iniciarDiagnostico(ordemId: os.id) } }
                        }
                    }
                    if capacidades.permite(.incluirServico) {
                        Button("Incluir Serviço") { incluindoServico = true }
                    }
                    if capacidades.permite(.removerServico) && !os.servicos.isEmpty {
                        Menu("Remover Serviço") {
                            ForEach(os.servicos) { servico in
                                Button(servico.nome, role: .destructive) { servicoARemover = servico }
                            }
                        }
                    }
                    if capacidades.permite(.incluirPecaInsumo) {
                        Button("Incluir Peça/insumo") { incluindoPecaInsumo = true }
                    }
                    if capacidades.permite(.removerPecaInsumo) && !os.pecasInsumos.isEmpty {
                        Menu("Remover Peça/insumo") {
                            ForEach(os.pecasInsumos) { item in
                                Button(item.nome, role: .destructive) { pecaInsumoARemover = item }
                            }
                        }
                    }
                    if capacidades.permite(.concluirDiagnostico) {
                        Button("Concluir diagnóstico") { confirmandoConclusao = true }
                    }
                }
                .disabled(executando)
            }
        }
        .alert("Não foi possível concluir a ação", isPresented: Binding(get: { mensagemDeErro != nil }, set: { if !$0 { mensagemDeErro = nil } })) {
            Button("OK", role: .cancel) {}
        } message: {
            Text(mensagemDeErro ?? "")
        }
        .confirmationDialog(
            "Remover o Serviço \(servicoARemover?.nome ?? "")?",
            isPresented: Binding(get: { servicoARemover != nil }, set: { if !$0 { servicoARemover = nil } }),
            titleVisibility: .visible,
            presenting: servicoARemover
        ) { servico in
            Button("Remover Serviço", role: .destructive) {
                Task { _ = await executar { try await ClienteApi(sessao: sessao).removerServico(ordemId: os.id, servicoId: servico.servicoId) } }
            }
        }
        .confirmationDialog("Concluir o diagnóstico?", isPresented: $confirmandoConclusao, titleVisibility: .visible) {
            Button("Concluir diagnóstico") {
                Task { _ = await executar { try await ClienteApi(sessao: sessao).concluirDiagnostico(ordemId: os.id) } }
            }
        } message: {
            Text("Será gerada uma Versão do orçamento e a OS aguardará a aprovação do Cliente.")
        }
        .sheet(isPresented: $incluindoServico) {
            IncluirServicoView(ordemId: os.id, aoExecutar: executar)
        }
        .sheet(isPresented: $incluindoPecaInsumo) {
            IncluirPecaInsumoView(ordemId: os.id, aoExecutar: executar)
        }
        .sheet(item: $pecaInsumoARemover) { item in
            RemoverPecaInsumoView(ordemId: os.id, item: item, aoExecutar: executar)
        }
    }

    private func executar(_ operacao: @MainActor () async throws -> Void) async -> String? {
        executando = true
        var mensagem: String?
        do {
            try await operacao()
        } catch {
            mensagem = error.localizedDescription
        }
        await recarregar()
        executando = false
        if let mensagem, !incluindoServico, !incluindoPecaInsumo, pecaInsumoARemover == nil {
            mensagemDeErro = mensagem
        }
        return mensagem
    }
}

typealias ExecutorDeAcao = @MainActor (@MainActor () async throws -> Void) async -> String?

private struct IncluirServicoView: View {
    @Environment(Sessao.self) private var sessao
    @Environment(\.dismiss) private var dismiss

    let ordemId: UUID
    let aoExecutar: ExecutorDeAcao

    @State private var servicos: [Servico] = []
    @State private var carregado = false
    @State private var erro: String?

    var body: some View {
        NavigationStack {
            List(servicos) { servico in
                Button {
                    Task {
                        erro = await aoExecutar { try await ClienteApi(sessao: sessao).incluirServico(ordemId: ordemId, servicoId: servico.id) }
                        if erro == nil { dismiss() }
                    }
                } label: {
                    LabeledContent(servico.nome, value: Formatacao.moeda(servico.preco))
                }
            }
            .overlay {
                if carregado && servicos.isEmpty {
                    ContentUnavailableView("Nenhum Serviço ativo", systemImage: "wrench.and.screwdriver")
                } else if !carregado {
                    ProgressView()
                }
            }
            .safeAreaInset(edge: .bottom) { MensagemDeErroView(texto: erro) }
            .navigationTitle("Incluir Serviço")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .cancellationAction) { Button("Cancelar") { dismiss() } } }
            .task { await carregar() }
        }
    }

    private func carregar() async {
        do {
            servicos = try await ClienteApi(sessao: sessao).listarServicos().filter(\.ativo)
        } catch {
            erro = error.localizedDescription
        }
        carregado = true
    }
}

private struct IncluirPecaInsumoView: View {
    @Environment(Sessao.self) private var sessao
    @Environment(\.dismiss) private var dismiss

    let ordemId: UUID
    let aoExecutar: ExecutorDeAcao

    @State private var itens: [PecaInsumo] = []
    @State private var carregado = false
    @State private var selecionado: PecaInsumo?
    @State private var texto = ""
    @State private var erro: String?

    var body: some View {
        NavigationStack {
            List(itens) { item in
                Button {
                    selecionado = item
                    erro = nil
                } label: {
                    HStack {
                        VStack(alignment: .leading, spacing: 2) {
                            Text(item.nome)
                            Text("\(item.tipo.rotulo) · saldo \(Formatacao.quantidade(item.saldo)) \(item.unidadeDeMedida) · \(Formatacao.moeda(item.preco))")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                        Spacer()
                        if selecionado?.id == item.id { Image(systemName: "checkmark") }
                    }
                }
                .foregroundStyle(.primary)
            }
            .overlay {
                if carregado && itens.isEmpty {
                    ContentUnavailableView("Nenhuma Peça/insumo ativa", systemImage: "shippingbox")
                } else if !carregado {
                    ProgressView()
                }
            }
            .safeAreaInset(edge: .bottom) {
                VStack(spacing: 8) {
                    MensagemDeErroView(texto: erro)
                    if let selecionado {
                        HStack {
                            TextField("Quantidade", text: $texto)
                                .keyboardType(selecionado.tipo == .insumo ? .decimalPad : .numberPad)
                                .textFieldStyle(.roundedBorder)
                            Text(selecionado.unidadeDeMedida)
                            Button("Incluir") { Task { await incluir(selecionado) } }
                                .buttonStyle(.borderedProminent)
                        }
                        .padding(.horizontal)
                    }
                }
                .padding(.vertical, 8)
                .background(.bar)
            }
            .navigationTitle("Incluir Peça/insumo")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .cancellationAction) { Button("Cancelar") { dismiss() } } }
            .task { await carregar() }
        }
    }

    private func carregar() async {
        do {
            itens = try await ClienteApi(sessao: sessao).listarPecasInsumos().filter(\.ativo)
        } catch {
            erro = error.localizedDescription
        }
        carregado = true
    }

    private func incluir(_ item: PecaInsumo) async {
        guard let quantidade = QuantidadeDigitada.interpretar(texto, inteira: item.tipo == .peca) else {
            erro = item.tipo == .peca ? "Informe uma quantidade inteira maior que zero." : "Informe uma quantidade maior que zero."
            return
        }
        erro = await aoExecutar { try await ClienteApi(sessao: sessao).incluirPecaInsumo(ordemId: ordemId, pecaInsumoId: item.id, quantidade: quantidade) }
        if erro == nil {
            dismiss()
        } else {
            await carregar()
        }
    }
}

private struct RemoverPecaInsumoView: View {
    @Environment(Sessao.self) private var sessao
    @Environment(\.dismiss) private var dismiss

    let ordemId: UUID
    let item: PecaInsumoDaOs
    let aoExecutar: ExecutorDeAcao

    @State private var removerTudo = true
    @State private var texto = ""
    @State private var confirmando = false
    @State private var erro: String?

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    LabeledContent(item.nome, value: "\(Formatacao.quantidade(item.quantidade)) \(item.unidadeDeMedida)")
                }
                Section {
                    Picker("Quantidade a remover", selection: $removerTudo) {
                        Text("Tudo").tag(true)
                        Text("Parte").tag(false)
                    }
                    .pickerStyle(.segmented)
                    if !removerTudo {
                        HStack {
                            TextField("Quantidade", text: $texto)
                                .keyboardType(.decimalPad)
                            Text(item.unidadeDeMedida)
                        }
                    }
                } footer: {
                    Text("O saldo removido volta ao estoque (Estorno).")
                }
                if let erro {
                    Section { Text(erro).foregroundStyle(.red) }
                }
                Section {
                    Button("Remover", role: .destructive) { preparar() }
                }
            }
            .navigationTitle("Remover Peça/insumo")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .cancellationAction) { Button("Cancelar") { dismiss() } } }
            .confirmationDialog("Remover \(item.nome) da OS?", isPresented: $confirmando, titleVisibility: .visible) {
                Button("Remover", role: .destructive) { Task { await remover() } }
            }
        }
    }

    private func preparar() {
        erro = nil
        if !removerTudo, QuantidadeDigitada.interpretar(texto, inteira: false) == nil {
            erro = "Informe uma quantidade maior que zero."
            return
        }
        confirmando = true
    }

    private func remover() async {
        let quantidade = removerTudo ? nil : QuantidadeDigitada.interpretar(texto, inteira: false)
        erro = await aoExecutar { try await ClienteApi(sessao: sessao).removerPecaInsumo(ordemId: ordemId, pecaInsumoId: item.pecaInsumoId, quantidade: quantidade) }
        if erro == nil { dismiss() }
    }
}

private struct MensagemDeErroView: View {
    let texto: String?

    var body: some View {
        if let texto {
            Text(texto)
                .font(.footnote)
                .foregroundStyle(.red)
                .padding(.horizontal)
                .frame(maxWidth: .infinity, alignment: .leading)
        }
    }
}

enum QuantidadeDigitada {
    static func interpretar(_ texto: String, inteira: Bool) -> Decimal? {
        let limpo = texto.trimmingCharacters(in: .whitespaces)
        let normalizado = limpo.replacingOccurrences(of: ",", with: ".")
        guard normalizado.allSatisfy({ $0.isASCII && ($0.isNumber || $0 == ".") }),
              normalizado.filter({ $0 == "." }).count <= 1,
              let valor = Decimal(string: normalizado, locale: Locale(identifier: "en_US_POSIX")),
              valor > 0
        else { return nil }
        if inteira && valor != valor.rounded(0) { return nil }
        return valor
    }
}

private extension Decimal {
    func rounded(_ casas: Int) -> Decimal {
        var origem = self
        var resultado = Decimal()
        NSDecimalRound(&resultado, &origem, casas, .plain)
        return resultado
    }
}
