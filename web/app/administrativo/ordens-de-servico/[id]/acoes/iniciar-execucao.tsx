import { Play } from "lucide-react";
import { iniciarExecucao } from "./acoes-da-execucao";
import { BotaoDeAcaoDaOs } from "./botao-de-acao-da-os";
import type { PropsDaAcaoDaOs } from "./tipos";

export function IniciarExecucao({ os }: PropsDaAcaoDaOs) {
  return (
    <BotaoDeAcaoDaOs
      rotulo="Iniciar execução"
      rotuloPendente="Iniciando..."
      icone={<Play />}
      acao={iniciarExecucao.bind(null, os.id)}
    />
  );
}
