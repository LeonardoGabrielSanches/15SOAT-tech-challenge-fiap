"use client";

import { useState, useTransition, type ComponentProps, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import type { Falha, Resultado } from "@/lib/api/resultado";
import { FalhaDaAcao } from "./falha-da-acao";

type Props = {
  rotulo: string;
  rotuloPendente: string;
  icone?: ReactNode;
  variant?: ComponentProps<typeof Button>["variant"];
  acao: () => Promise<Resultado<undefined>>;
};

export function BotaoDeAcaoDaOs({ rotulo, rotuloPendente, icone, variant, acao }: Props) {
  const [falha, setFalha] = useState<Falha | null>(null);
  const [pendente, startTransition] = useTransition();

  function executar() {
    startTransition(async () => {
      const resultado = await acao();
      startTransition(() => setFalha(resultado.ok ? null : resultado.falha));
    });
  }

  return (
    <div className="contents">
      <Button type="button" variant={variant} disabled={pendente} onClick={executar}>
        {icone}
        {pendente ? rotuloPendente : rotulo}
      </Button>
      {falha && (
        <div className="basis-full">
          <FalhaDaAcao falha={falha} />
        </div>
      )}
    </div>
  );
}
