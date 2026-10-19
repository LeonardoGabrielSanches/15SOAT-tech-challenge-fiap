import SwiftUI

struct RaizView: View {
    @Environment(Sessao.self) private var sessao

    var body: some View {
        switch sessao.estado {
        case .carregando:
            ProgressView("Carregando…")
        case .desconectado:
            LoginView()
        case let .conectado(usuario):
            if usuario.papel == nil {
                SemAcessoView(usuario: usuario)
            } else {
                InicioView(usuario: usuario)
            }
        }
    }
}
