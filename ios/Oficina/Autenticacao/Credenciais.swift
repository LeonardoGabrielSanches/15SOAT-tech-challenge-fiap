import Foundation

struct Credenciais: Codable, Sendable {
    let tokenDeAcesso: String
    let tokenDeRenovacao: String
    let tokenDeIdentidade: String?
    let expiraEm: Date
}

enum Papel: String, Sendable {
    case mecanico
    case gerente

    var rotulo: String {
        switch self {
        case .mecanico: "Mecânico"
        case .gerente: "Gerente"
        }
    }
}

struct Usuario: Sendable, Equatable {
    let nome: String
    let papeis: [String]

    var papel: Papel? {
        if papeis.contains(Papel.gerente.rawValue) { return .gerente }
        if papeis.contains(Papel.mecanico.rawValue) { return .mecanico }
        return nil
    }
}
