import Foundation

enum AcaoDaOs: Hashable, Sendable {
    case iniciarDiagnostico
    case incluirServico
    case removerServico
    case incluirPecaInsumo
    case removerPecaInsumo
    case concluirDiagnostico
    case iniciarExecucao
    case finalizarExecucao
}

struct CapacidadesDaOs: Sendable {
    let acoes: Set<AcaoDaOs>

    init(status: StatusDaOs?) {
        switch status {
        case .recebida:
            acoes = [.iniciarDiagnostico]
        case .emDiagnostico:
            acoes = [.incluirServico, .removerServico, .incluirPecaInsumo, .removerPecaInsumo, .concluirDiagnostico]
        case .aprovada:
            acoes = [.iniciarExecucao]
        case .emExecucao:
            acoes = [.finalizarExecucao]
        default:
            acoes = []
        }
    }

    func permite(_ acao: AcaoDaOs) -> Bool { acoes.contains(acao) }
}
