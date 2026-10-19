import Foundation

struct Servico: Decodable, Identifiable, Sendable {
    let id: UUID
    let nome: String
    let descricao: String?
    let preco: Decimal
    let ativo: Bool
}

extension ClienteApi {
    func listarServicos() async throws -> [Servico] {
        try await obter("api/administrativo/servicos")
    }
}
