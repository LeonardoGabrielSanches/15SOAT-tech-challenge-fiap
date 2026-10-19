import SwiftUI

struct AcoesDeExecucaoView: View {
    @Environment(Sessao.self) private var sessao

    let os: OrdemDeServicoDetalhe
    let aoAtualizar: () async -> Void

    @State private var emAndamento = false
    @State private var confirmandoFinalizacao = false
    @State private var erro: String?

    var body: some View {
        let capacidades = CapacidadesDaOs(status: os.statusDaOs)
        if capacidades.permite(.iniciarExecucao) || capacidades.permite(.finalizarExecucao) {
            Section("Execução") {
                if capacidades.permite(.iniciarExecucao) {
                    Button("Iniciar execução") {
                        Task { await executar { try await $0.iniciarExecucao(id: os.id) } }
                    }
                    .disabled(emAndamento)
                }
                if capacidades.permite(.finalizarExecucao) {
                    Button("Finalizar execução") { confirmandoFinalizacao = true }
                        .disabled(emAndamento)
                }
            }
            .confirmationDialog("Finalizar a execução desta OS?", isPresented: $confirmandoFinalizacao, titleVisibility: .visible) {
                Button("Finalizar execução") {
                    Task { await executar { try await $0.finalizarExecucao(id: os.id) } }
                }
                Button("Cancelar", role: .cancel) {}
            }
            .alert("Não foi possível concluir a ação", isPresented: Binding(get: { erro != nil }, set: { if !$0 { erro = nil } })) {
                Button("OK", role: .cancel) {}
            } message: {
                Text(erro ?? "")
            }
        }
    }

    private func executar(_ acao: (ClienteApi) async throws -> Void) async {
        emAndamento = true
        defer { emAndamento = false }
        do {
            try await acao(ClienteApi(sessao: sessao))
        } catch {
            erro = error.localizedDescription
        }
        await aoAtualizar()
    }
}
