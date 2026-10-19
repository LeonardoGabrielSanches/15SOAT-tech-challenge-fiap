import Foundation

enum Configuracao {
    static let urlApi = URL(string: "http://localhost:8081")!
    static let urlKeycloak = URL(string: "http://localhost:8080/realms/oficina")!
    static let clientId = "oficina-ios"
    static let esquemaDeRedirecionamento = "oficina"
    static let urlDeRedirecionamento = "oficina://autenticacao"
}
