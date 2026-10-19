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

    private func enviar(metodo: String, caminho: String, consulta: [URLQueryItem]) async throws -> Data {
        let token = try await tokenDaSessao { try await sessao.tokenDeAcesso() }
        let (dados, status) = try await executar(metodo: metodo, caminho: caminho, consulta: consulta, token: token)
        if status != 401 { return try validar(dados, status: status) }

        let novoToken = try await tokenDaSessao { try await sessao.renovarAposNaoAutorizado() }
        let (novosDados, novoStatus) = try await executar(metodo: metodo, caminho: caminho, consulta: consulta, token: novoToken)
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

    private func executar(metodo: String, caminho: String, consulta: [URLQueryItem], token: String) async throws -> (Data, Int) {
        var url = urlBase.appending(path: caminho)
        if !consulta.isEmpty { url.append(queryItems: consulta) }
        var requisicao = URLRequest(url: url)
        requisicao.httpMethod = metodo
        requisicao.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        requisicao.setValue("application/json", forHTTPHeaderField: "Accept")
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
        if let problema = try? JSONDecoder().decode(ProblemDetails.self, from: dados), let titulo = problema.title {
            throw ErroApi.problema(titulo: titulo, detalhe: problema.detail)
        }
        throw ErroApi.problema(titulo: "A API recusou a requisição (HTTP \(status))", detalhe: nil)
    }
}
