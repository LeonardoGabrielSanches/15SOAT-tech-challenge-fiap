import SwiftUI

struct LinhaDeOrdemView: View {
    let ordem: OrdemDeServicoResumo
    let mostraStatus: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(ordem.numero).font(.headline)
            if mostraStatus {
                Text(ordem.rotuloDoStatus).font(.subheadline)
            }
            Text("Aberta em \(DataDaApi.formatar(ordem.abertaEm))")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }
}
