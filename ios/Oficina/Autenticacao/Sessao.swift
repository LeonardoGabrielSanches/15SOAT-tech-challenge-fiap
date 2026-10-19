import Foundation
import Observation

@MainActor
@Observable
final class Sessao {
    enum Estado: Equatable {
        case carregando
        case desconectado
        case conectado(Usuario)
    }

    private(set) var estado: Estado = .carregando
    private(set) var mensagemDeErro: String?
    private(set) var ocupada = false

    @ObservationIgnored private let chaveiro = Chaveiro()
    @ObservationIgnored private let keycloak = ClienteKeycloak()
    @ObservationIgnored private let navegador = NavegadorDeLogin()
    @ObservationIgnored private var credenciais: Credenciais?
    @ObservationIgnored private var renovacaoEmAndamento: Task<Credenciais, Error>?

    private let margemDeRenovacao: TimeInterval = 30

    func restaurar() async {
        guard estado == .carregando else { return }
        guard let salvas = chaveiro.ler() else {
            estado = .desconectado
            return
        }
        credenciais = salvas
        do {
            _ = try await tokenDeAcesso()
            if let atuais = credenciais, let usuario = TokenDeAcesso.usuario(de: atuais.tokenDeAcesso) {
                estado = .conectado(usuario)
            } else {
                limpar()
            }
        } catch ErroDeAutenticacao.rede {
            if let usuario = TokenDeAcesso.usuario(de: salvas.tokenDeAcesso) {
                estado = .conectado(usuario)
                mensagemDeErro = ErroDeAutenticacao.rede.errorDescription
            } else {
                estado = .desconectado
            }
        } catch {
            limpar()
        }
    }

    func entrar() async {
        ocupada = true
        mensagemDeErro = nil
        defer { ocupada = false }
        do {
            let pkce = Pkce.gerar()
            let retorno = try await navegador.abrir(keycloak.urlDeAutorizacao(pkce: pkce))
            let itens = URLComponents(url: retorno, resolvingAgainstBaseURL: false)?.queryItems ?? []
            guard itens.first(where: { $0.name == "state" })?.value == pkce.estado,
                  let codigo = itens.first(where: { $0.name == "code" })?.value
            else { throw ErroDeAutenticacao.recusado }
            let novas = try await keycloak.trocarCodigo(codigo, verificador: pkce.verificador)
            try aceitar(novas)
        } catch ErroDeAutenticacao.cancelado {
            mensagemDeErro = nil
        } catch {
            mensagemDeErro = error.localizedDescription
        }
    }

    func sair() async {
        ocupada = true
        let tokenDeIdentidade = credenciais?.tokenDeIdentidade
        limpar()
        _ = try? await navegador.abrir(keycloak.urlDeEncerramento(tokenDeIdentidade: tokenDeIdentidade))
        ocupada = false
    }

    func tokenDeAcesso() async throws -> String {
        guard let atuais = credenciais else { throw ErroDeAutenticacao.recusado }
        if atuais.expiraEm.timeIntervalSinceNow > margemDeRenovacao {
            return atuais.tokenDeAcesso
        }
        return try await renovar().tokenDeAcesso
    }

    func renovarAposNaoAutorizado() async throws -> String {
        try await renovar().tokenDeAcesso
    }

    private func renovar() async throws -> Credenciais {
        if let emAndamento = renovacaoEmAndamento {
            return try await emAndamento.value
        }
        guard let atuais = credenciais else { throw ErroDeAutenticacao.recusado }
        let cliente = keycloak
        let tarefa = Task { try await cliente.renovar(tokenDeRenovacao: atuais.tokenDeRenovacao) }
        renovacaoEmAndamento = tarefa
        defer { renovacaoEmAndamento = nil }
        do {
            let novas = try await tarefa.value
            try aceitar(novas)
            return novas
        } catch ErroDeAutenticacao.rede {
            throw ErroDeAutenticacao.rede
        } catch {
            limpar()
            throw error
        }
    }

    private func aceitar(_ novas: Credenciais) throws {
        guard let usuario = TokenDeAcesso.usuario(de: novas.tokenDeAcesso) else {
            throw ErroDeAutenticacao.respostaInvalida
        }
        credenciais = novas
        chaveiro.gravar(novas)
        mensagemDeErro = nil
        if estado != .conectado(usuario) { estado = .conectado(usuario) }
    }

    private func limpar() {
        credenciais = nil
        chaveiro.apagar()
        estado = .desconectado
    }
}
