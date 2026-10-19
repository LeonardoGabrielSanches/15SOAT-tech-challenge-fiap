import { acoesPermitidasNaOs, permiteAcaoNaOs, type AcaoDaOs } from "@/lib/capacidades";
import { CancelarOs } from "./cancelar-os";
import { ConcluirDiagnostico } from "./concluir-diagnostico";
import { FinalizarExecucao } from "./finalizar-execucao";
import { IncluirPecaInsumo } from "./incluir-peca-insumo";
import { IncluirServico } from "./incluir-servico";
import { IniciarDiagnostico } from "./iniciar-diagnostico";
import { IniciarExecucao } from "./iniciar-execucao";
import { RegistrarEntrega } from "./registrar-entrega";
import { RemoverPecaInsumo } from "./remover-peca-insumo";
import { RemoverServico } from "./remover-servico";
import type { ComponenteDaAcao, PropsDaAcaoDaOs, PropsDaAcaoNaPecaInsumo, PropsDaAcaoNoServico } from "./tipos";

type AcaoDeStatus = Exclude<AcaoDaOs, "incluirServico" | "removerServico" | "incluirPecaInsumo" | "removerPecaInsumo">;

const acoesDeStatus: Record<AcaoDeStatus, ComponenteDaAcao<PropsDaAcaoDaOs>> = {
  iniciarDiagnostico: IniciarDiagnostico,
  concluirDiagnostico: ConcluirDiagnostico,
  iniciarExecucao: IniciarExecucao,
  finalizarExecucao: FinalizarExecucao,
  registrarEntrega: RegistrarEntrega,
  cancelar: CancelarOs,
};

const IncluirServicoPermitido: ComponenteDaAcao<PropsDaAcaoDaOs> = IncluirServico;
const IncluirPecaInsumoPermitida: ComponenteDaAcao<PropsDaAcaoDaOs> = IncluirPecaInsumo;
const RemoverServicoPermitido: ComponenteDaAcao<PropsDaAcaoNoServico> = RemoverServico;
const RemoverPecaInsumoPermitida: ComponenteDaAcao<PropsDaAcaoNaPecaInsumo> = RemoverPecaInsumo;

function ehAcaoDeStatus(acao: AcaoDaOs): acao is AcaoDeStatus {
  return acao in acoesDeStatus;
}

export function AcoesDeStatusDaOs({ os, papel }: PropsDaAcaoDaOs) {
  return (
    <div className="flex flex-wrap items-start gap-2 empty:hidden">
      {acoesPermitidasNaOs(os.status, papel)
        .filter(ehAcaoDeStatus)
        .map((acao) => {
          const Acao = acoesDeStatus[acao];
          return <Acao key={acao} os={os} papel={papel} />;
        })}
    </div>
  );
}

export function IncluirServicoNaOs({ os, papel }: PropsDaAcaoDaOs) {
  return permiteAcaoNaOs(os.status, papel, "incluirServico") ? <IncluirServicoPermitido os={os} papel={papel} /> : null;
}

export function IncluirPecaInsumoNaOs({ os, papel }: PropsDaAcaoDaOs) {
  return permiteAcaoNaOs(os.status, papel, "incluirPecaInsumo") ? (
    <IncluirPecaInsumoPermitida os={os} papel={papel} />
  ) : null;
}

export function RemoverServicoDaOs(props: PropsDaAcaoNoServico) {
  return <RemoverServicoPermitido {...props} />;
}

export function RemoverPecaInsumoDaOs(props: PropsDaAcaoNaPecaInsumo) {
  return <RemoverPecaInsumoPermitida {...props} />;
}
