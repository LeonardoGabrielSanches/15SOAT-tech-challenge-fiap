import Foundation

struct ClienteDaOs: Decodable, Sendable {
    let nome: String
}

struct VeiculoDaOs: Decodable, Sendable {
    let placa: String
    let marca: String
    let modelo: String
    let anoFabricacao: Int
}

struct ServicoDaOs: Decodable, Identifiable, Sendable {
    let servicoId: UUID
    let nome: String
    let preco: Decimal

    var id: UUID { servicoId }
}

struct PecaInsumoDaOs: Decodable, Identifiable, Sendable {
    let pecaInsumoId: UUID
    let nome: String
    let unidadeDeMedida: String
    let quantidade: Decimal
    let preco: Decimal

    var id: UUID { pecaInsumoId }
}

struct ItemDaVersao: Decodable, Identifiable, Sendable {
    let tipo: String
    let referenciaId: UUID
    let descricao: String
    let quantidade: Decimal
    let precoUnitario: Decimal
    let subtotal: Decimal

    var id: String { "\(tipo)-\(referenciaId)" }
}

struct VersaoDoOrcamento: Decodable, Identifiable, Sendable {
    let numero: Int
    let total: Decimal
    let situacao: String
    let motivoDaRecusa: String?
    let itens: [ItemDaVersao]

    var id: Int { numero }
}

struct EntradaDoHistorico: Decodable, Identifiable, Sendable {
    let origem: String?
    let destino: String
    let momento: Date
    let autor: String

    var id: String { "\(momento.timeIntervalSince1970)-\(destino)" }

    var rotuloDaOrigem: String { origem.map(Self.rotulo) ?? "Abertura" }
    var rotuloDoDestino: String { Self.rotulo(destino) }

    private static func rotulo(_ status: String) -> String {
        StatusDaOs(rawValue: status)?.rotulo ?? status
    }

    private enum CodingKeys: String, CodingKey {
        case origem, destino, momento, autor
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        origem = try container.decodeIfPresent(String.self, forKey: .origem)
        destino = try container.decode(String.self, forKey: .destino)
        autor = try container.decode(String.self, forKey: .autor)
        let texto = try container.decode(String.self, forKey: .momento)
        guard let data = DataDaApi.interpretar(texto) else {
            throw DecodingError.dataCorruptedError(forKey: .momento, in: container, debugDescription: "Data inválida")
        }
        momento = data
    }
}

struct OrdemDeServicoDetalhe: Decodable, Sendable {
    let id: UUID
    let numero: String
    let status: String
    let abertaEm: Date
    let cliente: ClienteDaOs
    let veiculo: VeiculoDaOs
    let servicos: [ServicoDaOs]
    let pecasInsumos: [PecaInsumoDaOs]
    let versoesDoOrcamento: [VersaoDoOrcamento]
    let historico: [EntradaDoHistorico]
    let motivoDaUltimaRecusa: String?

    var statusDaOs: StatusDaOs? { StatusDaOs(rawValue: status) }
    var rotuloDoStatus: String { statusDaOs?.rotulo ?? status }

    private enum CodingKeys: String, CodingKey {
        case id, numero, status, abertaEm, cliente, veiculo, servicos, pecasInsumos, versoesDoOrcamento, historico, motivoDaUltimaRecusa
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decode(UUID.self, forKey: .id)
        numero = try container.decode(String.self, forKey: .numero)
        status = try container.decode(String.self, forKey: .status)
        cliente = try container.decode(ClienteDaOs.self, forKey: .cliente)
        veiculo = try container.decode(VeiculoDaOs.self, forKey: .veiculo)
        servicos = try container.decode([ServicoDaOs].self, forKey: .servicos)
        pecasInsumos = try container.decode([PecaInsumoDaOs].self, forKey: .pecasInsumos)
        versoesDoOrcamento = try container.decode([VersaoDoOrcamento].self, forKey: .versoesDoOrcamento)
        historico = try container.decode([EntradaDoHistorico].self, forKey: .historico)
        motivoDaUltimaRecusa = try container.decodeIfPresent(String.self, forKey: .motivoDaUltimaRecusa)
        let texto = try container.decode(String.self, forKey: .abertaEm)
        guard let data = DataDaApi.interpretar(texto) else {
            throw DecodingError.dataCorruptedError(forKey: .abertaEm, in: container, debugDescription: "Data inválida")
        }
        abertaEm = data
    }
}

extension ClienteApi {
    func detalharOrdemDeServico(id: UUID) async throws -> OrdemDeServicoDetalhe {
        try await obter("api/administrativo/ordens-de-servico/\(id.uuidString.lowercased())")
    }
}
