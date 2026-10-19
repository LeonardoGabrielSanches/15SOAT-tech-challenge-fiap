import SwiftUI

@main
struct OficinaApp: App {
    @State private var sessao = Sessao()

    var body: some Scene {
        WindowGroup {
            RaizView()
                .environment(sessao)
                .task { await sessao.restaurar() }
        }
    }
}
