"use client";

import { PackageCheck } from "lucide-react";
import { registrarEntrega } from "./acoes-do-atendente";
import { BotaoDeAcaoDaOs } from "./botao-de-acao-da-os";
import type { PropsDaAcaoDaOs } from "./tipos";

export function RegistrarEntrega({ os }: PropsDaAcaoDaOs) {
  return (
    <BotaoDeAcaoDaOs
      rotulo="Registrar entrega"
      rotuloPendente="Registrando entrega..."
      icone={<PackageCheck />}
      acao={() => registrarEntrega(os.id)}
    />
  );
}
