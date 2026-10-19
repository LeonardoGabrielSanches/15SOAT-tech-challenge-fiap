import CryptoKit
import Foundation

enum ErroDeAutenticacao: LocalizedError {
    case cancelado
    case rede
    case recusado
    case respostaInvalida

    var errorDescription: String? {
        switch self {
        case .cancelado: "O login foi cancelado."
        case .rede: "Não foi possível falar com o Keycloak. Verifique se o ambiente está rodando e tente de novo."
        case .recusado: "O Keycloak recusou o login ou a sessão expirou."
        case .respostaInvalida: "O Keycloak devolveu uma resposta inesperada."
        }
    }
}

struct Pkce: Sendable {
    let verificador: String
    let desafio: String
    let estado: String

    static func gerar() -> Pkce {
        let verificador = aleatorio(bytes: 32)
        let desafio = Data(SHA256.hash(data: Data(verificador.utf8))).base64UrlSemPreenchimento
        return Pkce(verificador: verificador, desafio: desafio, estado: aleatorio(bytes: 16))
    }

    private static func aleatorio(bytes: Int) -> String {
        var dados = Data(count: bytes)
        let resultado = dados.withUnsafeMutableBytes { SecRandomCopyBytes(kSecRandomDefault, bytes, $0.baseAddress!) }
        precondition(resultado == errSecSuccess)
        return dados.base64UrlSemPreenchimento
    }
}

extension Data {
    var base64UrlSemPreenchimento: String {
        base64EncodedString()
            .replacingOccurrences(of: "+", with: "-")
            .replacingOccurrences(of: "/", with: "_")
            .replacingOccurrences(of: "=", with: "")
    }
}

struct ClienteKeycloak: Sendable {
    private struct RespostaDeToken: Decodable {
        let access_token: String
        let refresh_token: String
        let id_token: String?
        let expires_in: Double
    }

    private var urlProtocolo: URL { Configuracao.urlKeycloak.appending(path: "protocol/openid-connect") }

    func urlDeAutorizacao(pkce: Pkce) -> URL {
        var componentes = URLComponents(url: urlProtocolo.appending(path: "auth"), resolvingAgainstBaseURL: false)!
        componentes.queryItems = [
            URLQueryItem(name: "client_id", value: Configuracao.clientId),
            URLQueryItem(name: "response_type", value: "code"),
            URLQueryItem(name: "scope", value: "openid profile"),
            URLQueryItem(name: "redirect_uri", value: Configuracao.urlDeRedirecionamento),
            URLQueryItem(name: "code_challenge", value: pkce.desafio),
            URLQueryItem(name: "code_challenge_method", value: "S256"),
            URLQueryItem(name: "state", value: pkce.estado)
        ]
        return componentes.url!
    }

    func urlDeEncerramento(tokenDeIdentidade: String?) -> URL {
        var componentes = URLComponents(url: urlProtocolo.appending(path: "logout"), resolvingAgainstBaseURL: false)!
        var itens = [
            URLQueryItem(name: "client_id", value: Configuracao.clientId),
            URLQueryItem(name: "post_logout_redirect_uri", value: Configuracao.urlDeRedirecionamento)
        ]
        if let tokenDeIdentidade {
            itens.append(URLQueryItem(name: "id_token_hint", value: tokenDeIdentidade))
        }
        componentes.queryItems = itens
        return componentes.url!
    }

    func trocarCodigo(_ codigo: String, verificador: String) async throws -> Credenciais {
        try await solicitar([
            "grant_type": "authorization_code",
            "client_id": Configuracao.clientId,
            "code": codigo,
            "redirect_uri": Configuracao.urlDeRedirecionamento,
            "code_verifier": verificador
        ])
    }

    func renovar(tokenDeRenovacao: String) async throws -> Credenciais {
        try await solicitar([
            "grant_type": "refresh_token",
            "client_id": Configuracao.clientId,
            "refresh_token": tokenDeRenovacao
        ])
    }

    private func solicitar(_ campos: [String: String]) async throws -> Credenciais {
        var requisicao = URLRequest(url: urlProtocolo.appending(path: "token"))
        requisicao.httpMethod = "POST"
        requisicao.setValue("application/x-www-form-urlencoded", forHTTPHeaderField: "Content-Type")
        var formulario = URLComponents()
        formulario.queryItems = campos.map { URLQueryItem(name: $0.key, value: $0.value) }
        requisicao.httpBody = Data((formulario.percentEncodedQuery ?? "").utf8)

        let dados: Data
        let resposta: URLResponse
        do {
            (dados, resposta) = try await URLSession.shared.data(for: requisicao)
        } catch {
            throw ErroDeAutenticacao.rede
        }
        guard let http = resposta as? HTTPURLResponse else { throw ErroDeAutenticacao.respostaInvalida }
        guard http.statusCode == 200 else { throw ErroDeAutenticacao.recusado }
        guard let token = try? JSONDecoder().decode(RespostaDeToken.self, from: dados) else {
            throw ErroDeAutenticacao.respostaInvalida
        }
        return Credenciais(
            tokenDeAcesso: token.access_token,
            tokenDeRenovacao: token.refresh_token,
            tokenDeIdentidade: token.id_token,
            expiraEm: Date().addingTimeInterval(token.expires_in)
        )
    }
}
