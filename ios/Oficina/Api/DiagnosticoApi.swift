import Foundation

extension ClienteApi {
    private func caminhoDaOs(_ id: UUID) -> String {
        "api/administrativo/ordens-de-servico/\(id.uuidString.lowercased())"
    }

    private func corpo(_ campos: [String: String]) -> Data {
        let json = campos.map { "\"\($0.key)\":\($0.value)" }.sorted().joined(separator: ",")
        return Data("{\(json)}".utf8)
    }

    func iniciarDiagnostico(ordemId: UUID) async throws {
        try await acionar("POST", "\(caminhoDaOs(ordemId))/diagnostico/iniciar")
    }

    func concluirDiagnostico(ordemId: UUID) async throws {
        try await acionar("POST", "\(caminhoDaOs(ordemId))/diagnostico/concluir")
    }

    func incluirServico(ordemId: UUID, servicoId: UUID) async throws {
        try await acionar("POST", "\(caminhoDaOs(ordemId))/servicos", corpo: corpo(["servicoId": "\"\(servicoId.uuidString.lowercased())\""]))
    }

    func removerServico(ordemId: UUID, servicoId: UUID) async throws {
        try await acionar("DELETE", "\(caminhoDaOs(ordemId))/servicos/\(servicoId.uuidString.lowercased())")
    }

    func incluirPecaInsumo(ordemId: UUID, pecaInsumoId: UUID, quantidade: Decimal) async throws {
        let campos = ["pecaInsumoId": "\"\(pecaInsumoId.uuidString.lowercased())\"", "quantidade": "\(quantidade)"]
        try await acionar("POST", "\(caminhoDaOs(ordemId))/pecas-insumos", corpo: corpo(campos))
    }

    func removerPecaInsumo(ordemId: UUID, pecaInsumoId: UUID, quantidade: Decimal?) async throws {
        let consulta = quantidade.map { [URLQueryItem(name: "quantidade", value: "\($0)")] } ?? []
        try await acionar("DELETE", "\(caminhoDaOs(ordemId))/pecas-insumos/\(pecaInsumoId.uuidString.lowercased())", consulta: consulta)
    }
}
