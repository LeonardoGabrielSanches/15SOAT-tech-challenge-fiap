import SwiftUI

struct OrdemProvisoriaView: View {
    let ordem: OrdemDeServicoResumo

    var body: some View {
        List {
            LabeledContent("Número da OS", value: ordem.numero)
            LabeledContent("Status", value: ordem.rotuloDoStatus)
            LabeledContent("Aberta em", value: DataDaApi.formatar(ordem.abertaEm))
        }
        .navigationTitle(ordem.numero)
        .navigationBarTitleDisplayMode(.inline)
    }
}
