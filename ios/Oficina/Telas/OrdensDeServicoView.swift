import SwiftUI

struct OrdensDeServicoView: View {
    @Environment(Sessao.self) private var sessao

    enum Filtro: Hashable {
        case pendentes
        case status(StatusDaOs)

        var rotulo: String {
            switch self {
            case .pendentes: "OS pendentes"
            case let .status(status): status.rotulo
            }
        }
    }

    @State private var filtro: Filtro = .pendentes
    @State private var busca = ""
    @State private var ordens: [OrdemDeServicoResumo] = []
    @State private var ultimaPagina = 0
    @State private var total = 0
    @State private var carregandoMais = false
    @State private var carregada = false
    @State private var erro: String?

    private var termoDeBusca: String { busca.trimmingCharacters(in: .whitespaces) }

    private var visiveis: [OrdemDeServicoResumo] {
        guard !termoDeBusca.isEmpty else { return ordens }
        return ordens.filter { $0.numero.localizedCaseInsensitiveContains(termoDeBusca) }
    }

    private var temMais: Bool { carregada && ordens.count < total }

    private var statusDoFiltro: StatusDaOs? {
        if case let .status(escolhido) = filtro { escolhido } else { nil }
    }

    var body: some View {
        List {
            ForEach(visiveis) { ordem in
                NavigationLink(value: ordem) {
                    LinhaDeOrdemView(ordem: ordem, mostraStatus: true)
                }
            }
            if temMais {
                Button {
                    Task { await carregarMais() }
                } label: {
                    HStack {
                        Text("Carregar mais (\(ordens.count) de \(total))")
                        if carregandoMais {
                            Spacer()
                            ProgressView()
                        }
                    }
                }
                .disabled(carregandoMais)
            }
        }
        .overlay { estadoVazio }
        .searchable(text: $busca, prompt: "Número da OS")
        .textInputAutocapitalization(.never)
        .autocorrectionDisabled()
        .navigationDestination(for: OrdemDeServicoResumo.self) { OrdemDeServicoView(ordem: $0) }
        .toolbar {
            ToolbarItem(placement: .topBarLeading) {
                Menu {
                    Picker("Status", selection: $filtro) {
                        Text(Filtro.pendentes.rotulo).tag(Filtro.pendentes)
                        ForEach(StatusDaOs.allCases) { status in
                            Text(status.rotulo).tag(Filtro.status(status))
                        }
                    }
                } label: {
                    Label(filtro.rotulo, systemImage: "line.3.horizontal.decrease.circle")
                }
            }
        }
        .refreshable { await carregar() }
        .task(id: filtro) {
            carregada = false
            await carregar()
        }
    }

    @ViewBuilder
    private var estadoVazio: some View {
        if let erro, ordens.isEmpty {
            ContentUnavailableView("Não foi possível carregar", systemImage: "exclamationmark.triangle", description: Text(erro))
        } else if !carregada {
            ProgressView()
        } else if visiveis.isEmpty {
            if termoDeBusca.isEmpty {
                ContentUnavailableView("Nenhuma OS", systemImage: "tray", description: Text("Nenhuma OS corresponde a \"\(filtro.rotulo)\"."))
            } else {
                ContentUnavailableView.search(text: busca)
            }
        }
    }

    private func carregar() async {
        do {
            let primeira = try await ClienteApi(sessao: sessao).listarOrdensDeServico(status: statusDoFiltro, pagina: 1)
            ordens = primeira.itens
            ultimaPagina = primeira.pagina
            total = primeira.total
            erro = nil
        } catch {
            if !Task.isCancelled {
                ordens = []
                total = 0
                self.erro = error.localizedDescription
            }
        }
        if !Task.isCancelled { carregada = true }
    }

    private func carregarMais() async {
        carregandoMais = true
        defer { carregandoMais = false }
        do {
            let proxima = try await ClienteApi(sessao: sessao)
                .listarOrdensDeServico(status: statusDoFiltro, pagina: ultimaPagina + 1)
            let conhecidas = Set(ordens.map(\.id))
            ordens.append(contentsOf: proxima.itens.filter { !conhecidas.contains($0.id) })
            ultimaPagina = proxima.pagina
            total = proxima.total
            erro = nil
        } catch {
            self.erro = error.localizedDescription
        }
    }
}
