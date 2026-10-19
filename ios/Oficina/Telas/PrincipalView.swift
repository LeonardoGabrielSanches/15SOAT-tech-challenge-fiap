import SwiftUI

struct PrincipalView: View {
    let usuario: Usuario

    @State private var mostraConta = false

    var body: some View {
        TabView {
            aba("Fila de execução", icone: "list.number") { FilaDeExecucaoView() }
            aba("Ordens de serviço", icone: "wrench.and.screwdriver") { OrdensDeServicoView() }
        }
        .sheet(isPresented: $mostraConta) { ContaView(usuario: usuario) }
    }

    private func aba<Conteudo: View>(_ titulo: String, icone: String, @ViewBuilder conteudo: () -> Conteudo) -> some View {
        NavigationStack {
            conteudo()
                .navigationTitle(titulo)
                .toolbar {
                    ToolbarItem(placement: .topBarTrailing) {
                        Button { mostraConta = true } label: { Label("Conta", systemImage: "person.circle") }
                    }
                }
        }
        .tabItem { Label(titulo, systemImage: icone) }
    }
}

struct ContaView: View {
    @Environment(Sessao.self) private var sessao
    @Environment(\.dismiss) private var dispensar
    let usuario: Usuario

    var body: some View {
        NavigationStack {
            List {
                Section("Funcionário") {
                    LabeledContent("Nome", value: usuario.nome)
                    LabeledContent("Papel", value: usuario.papel?.rotulo ?? "")
                }
                if let mensagem = sessao.mensagemDeErro {
                    Section { Text(mensagem).foregroundStyle(.red) }
                }
                Section {
                    Button("Sair", role: .destructive) {
                        Task {
                            await sessao.sair()
                            dispensar()
                        }
                    }
                    .disabled(sessao.ocupada)
                }
            }
            .navigationTitle("Conta")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) { Button("Fechar") { dispensar() } }
            }
        }
        .presentationDetents([.medium])
    }
}
