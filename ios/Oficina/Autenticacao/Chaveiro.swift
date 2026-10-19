import Foundation
import Security

struct Chaveiro: Sendable {
    private let servico = "com.sleonardogabriel.oficina.credenciais"
    private let conta = "sessao"

    private var consulta: [String: Any] {
        [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: servico,
            kSecAttrAccount as String: conta
        ]
    }

    func ler() -> Credenciais? {
        var consulta = consulta
        consulta[kSecReturnData as String] = true
        consulta[kSecMatchLimit as String] = kSecMatchLimitOne
        var resultado: AnyObject?
        guard SecItemCopyMatching(consulta as CFDictionary, &resultado) == errSecSuccess,
              let dados = resultado as? Data
        else { return nil }
        return try? JSONDecoder().decode(Credenciais.self, from: dados)
    }

    func gravar(_ credenciais: Credenciais) {
        guard let dados = try? JSONEncoder().encode(credenciais) else { return }
        let atualizacao = [kSecValueData as String: dados]
        let status = SecItemUpdate(consulta as CFDictionary, atualizacao as CFDictionary)
        if status == errSecItemNotFound {
            var novo = consulta
            novo[kSecValueData as String] = dados
            novo[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
            SecItemAdd(novo as CFDictionary, nil)
        }
    }

    func apagar() {
        SecItemDelete(consulta as CFDictionary)
    }
}
