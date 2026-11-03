"use client";

import Link from "next/link";
import { useActionState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import type { EstadoDaDecisao } from "../acoes";
import { impedeNovaDecisao } from "../decisao-do-cliente";
import { FormularioDaDecisao } from "../formulario-da-decisao";

type Props = {
  decidir: (estado: EstadoDaDecisao, formData: FormData) => Promise<EstadoDaDecisao>;
};

export function DecisaoDoOrcamento({ decidir }: Props) {
  const [estado, acao, enviando] = useActionState(decidir, undefined);
  const falha = estado?.falha;

  if (falha && impedeNovaDecisao(falha)) {
    return (
      <div className="flex flex-col gap-4">
        <AvisoDeFalha falha={falha} />
        <Button asChild variant="outline" size="lg" className="w-full sm:w-auto sm:self-start">
          <Link href="/portal/aprovacao">Informar outro token</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {falha && <AvisoDeFalha falha={falha} />}
      <FormularioDaDecisao acao={acao} enviando={enviando} erroDoMotivo={estado?.erroDoMotivo} />
    </div>
  );
}
