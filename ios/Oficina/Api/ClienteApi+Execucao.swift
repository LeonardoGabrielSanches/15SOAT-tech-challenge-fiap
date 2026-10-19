import Foundation

extension ClienteApi {
    func iniciarExecucao(id: UUID) async throws {
        try await acionar("POST", "api/administrativo/ordens-de-servico/\(id.uuidString.lowercased())/execucao/iniciar")
    }

    func finalizarExecucao(id: UUID) async throws {
        try await acionar("POST", "api/administrativo/ordens-de-servico/\(id.uuidString.lowercased())/execucao/finalizar")
    }
}
