import AuthenticationServices
import UIKit

@MainActor
final class NavegadorDeLogin: NSObject, ASWebAuthenticationPresentationContextProviding {
    func abrir(_ url: URL) async throws -> URL {
        try await withCheckedThrowingContinuation { continuacao in
            let sessao = ASWebAuthenticationSession(
                url: url,
                callbackURLScheme: Configuracao.esquemaDeRedirecionamento
            ) { retorno, erro in
                if let retorno {
                    continuacao.resume(returning: retorno)
                } else if let erro = erro as? ASWebAuthenticationSessionError, erro.code == .canceledLogin {
                    continuacao.resume(throwing: ErroDeAutenticacao.cancelado)
                } else {
                    continuacao.resume(throwing: ErroDeAutenticacao.rede)
                }
            }
            sessao.presentationContextProvider = self
            sessao.prefersEphemeralWebBrowserSession = false
            if !sessao.start() {
                continuacao.resume(throwing: ErroDeAutenticacao.rede)
            }
        }
    }

    func presentationAnchor(for session: ASWebAuthenticationSession) -> ASPresentationAnchor {
        UIApplication.shared.connectedScenes
            .compactMap { ($0 as? UIWindowScene)?.keyWindow }
            .first ?? ASPresentationAnchor()
    }
}
