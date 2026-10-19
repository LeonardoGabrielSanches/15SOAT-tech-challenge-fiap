import { CircleCheck } from "lucide-react";
import { finalizarExecucao } from "./acoes-da-execucao";
import { BotaoDeAcaoDaOs } from "./botao-de-acao-da-os";
import type { PropsDaAcaoDaOs } from "./tipos";

export function FinalizarExecucao({ os }: PropsDaAcaoDaOs) {
  return (
    <BotaoDeAcaoDaOs
      rotulo="Finalizar execução"
      rotuloPendente="Finalizando..."
      icone={<CircleCheck />}
      acao={finalizarExecucao.bind(null, os.id)}
    />
  );
}
