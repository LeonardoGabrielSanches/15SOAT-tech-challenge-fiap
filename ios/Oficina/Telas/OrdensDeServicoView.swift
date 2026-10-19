import SwiftUI

struct OrdensDeServicoView: View {
    @Environment(Sessao.self) private var sessao

    enum Filtro: Hashable {
        case emAndamento
        case status(StatusDaOs)

        var rotulo: String {
            switch self {
            case .emAndamento: "OS em andamento"
            case let .status(status): status.rotulo
            }
        }
    }

    @State private var filtro: Filtro = .emAndamento
    @State private var busca = ""
    @State private var ordens: [OrdemDeServicoResumo] = []
    @State private var carregada = false
    @State private var erro: String?

    private var termoDeBusca: String { busca.trimmingCharacters(in: .whitespaces) }

    private var visiveis: [OrdemDeServicoResumo] {
        let doFiltro = filtro == .emAndamento ? ordens.filter { $0.statusDaOs?.emAndamento ?? true } : ordens
        guard !termoDeBusca.isEmpty else { return doFiltro }
        return doFiltro.filter { $0.numero.localizedCaseInsensitiveContains(termoDeBusca) }
    }

    var body: some View {
        List(visiveis) { ordem in
            NavigationLink(value: ordem) {
                LinhaDeOrdemView(ordem: ordem, mostraStatus: true)
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
                        Text(Filtro.emAndamento.rotulo).tag(Filtro.emAndamento)
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
        let status: StatusDaOs? = if case let .status(escolhido) = filtro { escolhido } else { nil }
        do {
            ordens = try await ClienteApi(sessao: sessao).listarOrdensDeServico(status: status)
            erro = nil
        } catch {
            if !Task.isCancelled {
                ordens = []
                self.erro = error.localizedDescription
            }
        }
        if !Task.isCancelled { carregada = true }
    }
}
