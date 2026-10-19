import SwiftUI

struct SemAcessoView: View {
    @Environment(Sessao.self) private var sessao
    let usuario: Usuario

    var body: some View {
        VStack(spacing: 20) {
            Image(systemName: "lock.fill")
                .font(.system(size: 56))
                .foregroundStyle(.secondary)
            Text("Sem acesso")
                .font(.title.bold())
            Text("\(usuario.nome), este app é para o Mecânico. Use a interface web.")
                .multilineTextAlignment(.center)
                .foregroundStyle(.secondary)
            Button("Sair") { Task { await sessao.sair() } }
                .buttonStyle(.borderedProminent)
                .disabled(sessao.ocupada)
        }
        .padding(32)
    }
}
