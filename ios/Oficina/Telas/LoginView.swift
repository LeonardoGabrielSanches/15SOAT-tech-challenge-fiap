import SwiftUI

struct LoginView: View {
    @Environment(Sessao.self) private var sessao

    var body: some View {
        VStack(spacing: 24) {
            Image(systemName: "wrench.and.screwdriver")
                .font(.system(size: 64))
                .foregroundStyle(.tint)
            Text("Oficina")
                .font(.largeTitle.bold())
            Text("Entre com seu usuário do Keycloak.")
                .foregroundStyle(.secondary)
            if let mensagem = sessao.mensagemDeErro {
                Text(mensagem)
                    .font(.callout)
                    .foregroundStyle(.red)
                    .multilineTextAlignment(.center)
            }
            Button {
                Task { await sessao.entrar() }
            } label: {
                if sessao.ocupada {
                    ProgressView()
                } else {
                    Text("Entrar").frame(maxWidth: .infinity)
                }
            }
            .buttonStyle(.borderedProminent)
            .controlSize(.large)
            .disabled(sessao.ocupada)
        }
        .padding(32)
    }
}
