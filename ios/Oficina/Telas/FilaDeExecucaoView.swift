import SwiftUI

struct FilaDeExecucaoView: View {
    @Environment(Sessao.self) private var sessao

    @State private var ordens: [OrdemDeServicoResumo] = []
    @State private var carregada = false
    @State private var erro: String?

    var body: some View {
        List(ordens) { ordem in
            NavigationLink(value: ordem) {
                LinhaDeOrdemView(ordem: ordem, mostraStatus: false)
            }
        }
        .overlay {
            if let erro, ordens.isEmpty {
                ContentUnavailableView("Não foi possível carregar", systemImage: "exclamationmark.triangle", description: Text(erro))
            } else if carregada && ordens.isEmpty {
                ContentUnavailableView("Fila de execução vazia", systemImage: "tray", description: Text("Nenhuma OS Aprovada aguarda execução."))
            } else if !carregada {
                ProgressView()
            }
        }
        .navigationDestination(for: OrdemDeServicoResumo.self) { OrdemDeServicoView(ordem: $0) }
        .refreshable { await carregar() }
        .task { await carregar() }
    }

    private func carregar() async {
        do {
            ordens = try await ClienteApi(sessao: sessao).listarFilaDeExecucao()
            erro = nil
        } catch {
            if !Task.isCancelled { self.erro = error.localizedDescription }
        }
        carregada = true
    }
}
