"use client";

import { Stethoscope } from "lucide-react";
import { iniciarDiagnostico } from "./acoes-do-diagnostico";
import { BotaoDeAcaoDaOs } from "./botao-de-acao-da-os";
import type { PropsDaAcaoDaOs } from "./tipos";

export function IniciarDiagnostico({ os }: PropsDaAcaoDaOs) {
  return (
    <BotaoDeAcaoDaOs
      rotulo="Iniciar diagnóstico"
      rotuloPendente="Iniciando..."
      icone={<Stethoscope />}
      acao={() => iniciarDiagnostico(os.id)}
    />
  );
}
