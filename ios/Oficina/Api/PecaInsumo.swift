import Foundation

enum TipoDePecaInsumo: String, Decodable, Sendable {
    case peca = "Peca"
    case insumo = "Insumo"

    var rotulo: String {
        switch self {
        case .peca: "Peça"
        case .insumo: "Insumo"
        }
    }
}

struct PecaInsumo: Decodable, Identifiable, Sendable {
    let id: UUID
    let nome: String
    let tipo: TipoDePecaInsumo
    let unidadeDeMedida: String
    let preco: Decimal
    let saldo: Decimal
    let ativo: Bool
}

extension ClienteApi {
    func listarPecasInsumos() async throws -> [PecaInsumo] {
        try await obter("api/pecas-insumos")
    }
}
