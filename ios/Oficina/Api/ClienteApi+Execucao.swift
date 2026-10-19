import Foundation

extension ClienteApi {
    func iniciarExecucao(id: UUID) async throws {
        try await postar("api/administrativo/ordens-de-servico/\(id.uuidString.lowercased())/execucao/iniciar")
    }

    func finalizarExecucao(id: UUID) async throws {
        try await postar("api/administrativo/ordens-de-servico/\(id.uuidString.lowercased())/execucao/finalizar")
    }
}
