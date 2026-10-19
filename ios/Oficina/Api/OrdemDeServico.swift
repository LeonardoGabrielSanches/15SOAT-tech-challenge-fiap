import Foundation

enum StatusDaOs: String, CaseIterable, Identifiable, Sendable {
    case recebida = "Recebida"
    case emDiagnostico = "EmDiagnostico"
    case aguardandoAprovacao = "AguardandoAprovacao"
    case aprovada = "Aprovada"
    case emExecucao = "EmExecucao"
    case finalizada = "Finalizada"
    case entregue = "Entregue"
    case cancelada = "Cancelada"

    var id: String { rawValue }

    var rotulo: String {
        switch self {
        case .recebida: "Recebida"
        case .emDiagnostico: "Em diagnóstico"
        case .aguardandoAprovacao: "Aguardando aprovação"
        case .aprovada: "Aprovada"
        case .emExecucao: "Em execução"
        case .finalizada: "Finalizada"
        case .entregue: "Entregue"
        case .cancelada: "Cancelada"
        }
    }

    var emAndamento: Bool { self != .entregue && self != .cancelada }
}

struct OrdemDeServicoResumo: Decodable, Identifiable, Hashable, Sendable {
    let id: UUID
    let numero: String
    let status: String
    let abertaEm: Date

    var statusDaOs: StatusDaOs? { StatusDaOs(rawValue: status) }
    var rotuloDoStatus: String { statusDaOs?.rotulo ?? status }

    private enum CodingKeys: String, CodingKey {
        case id, numero, status, abertaEm
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decode(UUID.self, forKey: .id)
        numero = try container.decode(String.self, forKey: .numero)
        status = try container.decode(String.self, forKey: .status)
        let texto = try container.decode(String.self, forKey: .abertaEm)
        guard let data = DataDaApi.interpretar(texto) else {
            throw DecodingError.dataCorruptedError(forKey: .abertaEm, in: container, debugDescription: "Data inválida")
        }
        abertaEm = data
    }
}

enum DataDaApi {
    static func interpretar(_ texto: String) -> Date? {
        let comFracao = Date.ISO8601FormatStyle(includingFractionalSeconds: true)
        if let data = try? comFracao.parse(texto) { return data }
        return try? Date.ISO8601FormatStyle().parse(texto)
    }

    static func formatar(_ data: Date) -> String {
        var estilo = Date.FormatStyle(date: .numeric, time: .shortened)
        estilo.locale = Locale(identifier: "pt_BR")
        estilo.timeZone = TimeZone(identifier: "America/Sao_Paulo") ?? .current
        return data.formatted(estilo)
    }
}

extension ClienteApi {
    func listarOrdensDeServico(status: StatusDaOs?) async throws -> [OrdemDeServicoResumo] {
        let consulta = status.map { [URLQueryItem(name: "status", value: $0.rawValue)] } ?? []
        return try await obter("api/administrativo/ordens-de-servico", consulta: consulta)
    }

    func listarFilaDeExecucao() async throws -> [OrdemDeServicoResumo] {
        try await obter("api/administrativo/ordens-de-servico/fila-de-execucao")
    }
}
