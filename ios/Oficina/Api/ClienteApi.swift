import Foundation

@MainActor
struct ClienteApi {
    let sessao: Sessao
    let urlBase: URL = Configuracao.urlApi

    func obter<T: Decodable>(_ caminho: String, consulta: [URLQueryItem] = []) async throws -> T {
        let dados = try await enviar(metodo: "GET", caminho: caminho, consulta: consulta)
        do {
            return try JSONDecoder().decode(T.self, from: dados)
        } catch {
            throw ErroApi.respostaInvalida
        }
    }

    func acionar(_ metodo: String, _ caminho: String, consulta: [URLQueryItem] = [], corpo: Data? = nil) async throws {
        _ = try await enviar(metodo: metodo, caminho: caminho, consulta: consulta, corpo: corpo)
    }

    private func enviar(metodo: String, caminho: String, consulta: [URLQueryItem], corpo: Data? = nil) async throws -> Data {
        let token = try await tokenDaSessao { try await sessao.tokenDeAcesso() }
        let (dados, status) = try await executar(metodo: metodo, caminho: caminho, consulta: consulta, corpo: corpo, token: token)
        if status != 401 { return try validar(dados, status: status) }

        let novoToken = try await tokenDaSessao { try await sessao.renovarAposNaoAutorizado(tokenRecusado: token) }
        let (novosDados, novoStatus) = try await executar(metodo: metodo, caminho: caminho, consulta: consulta, corpo: corpo, token: novoToken)
        return try validar(novosDados, status: novoStatus)
    }

    private func tokenDaSessao(_ obter: () async throws -> String) async throws -> String {
        do {
            return try await obter()
        } catch ErroDeAutenticacao.rede {
            throw ErroApi.semConexao
        } catch {
            throw ErroApi.sessaoExpirada
        }
    }

    private func executar(metodo: String, caminho: String, consulta: [URLQueryItem], corpo: Data?, token: String) async throws -> (Data, Int) {
        var url = urlBase.appending(path: caminho)
        if !consulta.isEmpty { url.append(queryItems: consulta) }
        var requisicao = URLRequest(url: url)
        requisicao.httpMethod = metodo
        requisicao.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        requisicao.setValue("application/json", forHTTPHeaderField: "Accept")
        if let corpo {
            requisicao.httpBody = corpo
            requisicao.setValue("application/json", forHTTPHeaderField: "Content-Type")
        }
        do {
            let (dados, resposta) = try await URLSession.shared.data(for: requisicao)
            guard let http = resposta as? HTTPURLResponse else { throw ErroApi.respostaInvalida }
            return (dados, http.statusCode)
        } catch let erro as ErroApi {
            throw erro
        } catch {
            throw ErroApi.semConexao
        }
    }

    private func validar(_ dados: Data, status: Int) throws -> Data {
        if (200..<300).contains(status) { return dados }
        if status == 401 { throw ErroApi.sessaoExpirada }
        if status == 404 { throw ErroApi.naoEncontrado }
        if status == 409 {
            let conflito = try? JSONDecoder().decode(ProblemDetails.self, from: dados)
            throw ErroApi.conflito(titulo: conflito?.title ?? "Conflito", detalhe: conflito?.detail)
        }
        if let problema = try? JSONDecoder().decode(ProblemDetails.self, from: dados), let titulo = problema.title {
            throw ErroApi.problema(titulo: titulo, detalhe: problema.detail)
        }
        throw ErroApi.problema(titulo: "A API recusou a requisição (HTTP \(status))", detalhe: nil)
    }
}
