import Foundation

extension ClienteApi {
    private func caminhoDaOs(_ id: UUID) -> String {
        "api/administrativo/ordens-de-servico/\(id.uuidString.lowercased())"
    }

    private struct CorpoDeServico: Encodable {
        let servicoId: UUID
    }

    private struct CorpoDePecaInsumo: Encodable {
        let pecaInsumoId: UUID
        let quantidade: Decimal
    }

    private func corpo<T: Encodable>(_ valor: T) throws -> Data {
        try JSONEncoder().encode(valor)
    }

    func iniciarDiagnostico(ordemId: UUID) async throws {
        try await acionar("POST", "\(caminhoDaOs(ordemId))/diagnostico/iniciar")
    }

    func concluirDiagnostico(ordemId: UUID) async throws {
        try await acionar("POST", "\(caminhoDaOs(ordemId))/diagnostico/concluir")
    }

    func incluirServico(ordemId: UUID, servicoId: UUID) async throws {
        try await acionar("POST", "\(caminhoDaOs(ordemId))/servicos", corpo: try corpo(CorpoDeServico(servicoId: servicoId)))
    }

    func removerServico(ordemId: UUID, servicoId: UUID) async throws {
        try await acionar("DELETE", "\(caminhoDaOs(ordemId))/servicos/\(servicoId.uuidString.lowercased())")
    }

    func incluirPecaInsumo(ordemId: UUID, pecaInsumoId: UUID, quantidade: Decimal) async throws {
        let dados = try corpo(CorpoDePecaInsumo(pecaInsumoId: pecaInsumoId, quantidade: quantidade))
        try await acionar("POST", "\(caminhoDaOs(ordemId))/pecas-insumos", corpo: dados)
    }

    func removerPecaInsumo(ordemId: UUID, pecaInsumoId: UUID, quantidade: Decimal?) async throws {
        let consulta = quantidade.map { [URLQueryItem(name: "quantidade", value: "\($0)")] } ?? []
        try await acionar("DELETE", "\(caminhoDaOs(ordemId))/pecas-insumos/\(pecaInsumoId.uuidString.lowercased())", consulta: consulta)
    }
}
