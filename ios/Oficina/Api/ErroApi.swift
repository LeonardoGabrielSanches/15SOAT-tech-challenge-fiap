import Foundation

enum ErroApi: LocalizedError {
    case problema(titulo: String, detalhe: String?)
    case semConexao
    case respostaInvalida
    case sessaoExpirada
    case naoEncontrado

    var errorDescription: String? {
        switch self {
        case let .problema(titulo, detalhe):
            if let detalhe, !detalhe.isEmpty { "\(titulo): \(detalhe)" } else { titulo }
        case .semConexao:
            "Não foi possível falar com a API. Verifique se o ambiente está rodando e tente de novo."
        case .respostaInvalida:
            "A API devolveu uma resposta inesperada."
        case .sessaoExpirada:
            "Sua sessão expirou. Entre de novo."
        case .naoEncontrado:
            "O registro não foi encontrado."
        }
    }
}

struct ProblemDetails: Decodable {
    let title: String?
    let detail: String?
}
