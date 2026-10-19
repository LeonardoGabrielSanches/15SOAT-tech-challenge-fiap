import Foundation

enum TokenDeAcesso {
    private struct Declaracoes: Decodable {
        struct AcessoDoReino: Decodable {
            let roles: [String]
        }

        let name: String?
        let preferred_username: String?
        let realm_access: AcessoDoReino?
    }

    static func usuario(de token: String) -> Usuario? {
        let partes = token.split(separator: ".")
        guard partes.count >= 2, let dados = decodificarBase64Url(String(partes[1])),
              let declaracoes = try? JSONDecoder().decode(Declaracoes.self, from: dados)
        else { return nil }
        let nome = declaracoes.name ?? declaracoes.preferred_username ?? "Funcionário"
        return Usuario(nome: nome, papeis: declaracoes.realm_access?.roles ?? [])
    }

    private static func decodificarBase64Url(_ texto: String) -> Data? {
        var base64 = texto.replacingOccurrences(of: "-", with: "+").replacingOccurrences(of: "_", with: "/")
        while base64.count % 4 != 0 { base64.append("=") }
        return Data(base64Encoded: base64)
    }
}
