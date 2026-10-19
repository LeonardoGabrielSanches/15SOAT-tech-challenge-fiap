import SwiftUI

struct InicioView: View {
    @Environment(Sessao.self) private var sessao
    let usuario: Usuario

    @State private var carga: Carga = .carregando

    enum Carga {
        case carregando
        case concluida(Int)
        case falhou(String)
    }

    var body: some View {
        NavigationStack {
            List {
                Section("Funcionário") {
                    LabeledContent("Nome", value: usuario.nome)
                    LabeledContent("Papel", value: usuario.papel?.rotulo ?? "")
                }
                Section("Conexão com a API") {
                    switch carga {
                    case .carregando:
                        ProgressView()
                    case let .concluida(quantidade):
                        Text("API autenticada: \(quantidade) Serviços no catálogo.")
                    case let .falhou(mensagem):
                        Text(mensagem).foregroundStyle(.red)
                    }
                }
                if let mensagem = sessao.mensagemDeErro {
                    Section { Text(mensagem).foregroundStyle(.red) }
                }
            }
            .navigationTitle("Oficina")
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Sair") { Task { await sessao.sair() } }
                        .disabled(sessao.ocupada)
                }
            }
            .refreshable { await carregar() }
            .task { await carregar() }
        }
    }

    private func carregar() async {
        do {
            let servicos = try await ClienteApi(sessao: sessao).listarServicos()
            carga = .concluida(servicos.count)
        } catch {
            carga = .falhou(error.localizedDescription)
        }
    }
}
