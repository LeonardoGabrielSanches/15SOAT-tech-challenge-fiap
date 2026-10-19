"use client";

import Link from "next/link";
import { useActionState, useState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { EstadoDaDecisao } from "../acoes";

const tamanhoMaximoDoMotivo = 500;

type Props = {
  aprovar: (estado: EstadoDaDecisao) => Promise<EstadoDaDecisao>;
  recusar: (estado: EstadoDaDecisao, formData: FormData) => Promise<EstadoDaDecisao>;
};

export function DecisaoDoOrcamento({ aprovar, recusar }: Props) {
  const [recusando, setRecusando] = useState(false);
  const [estadoDaAprovacao, acaoDeAprovar, aprovando] = useActionState(aprovar, undefined);
  const [estadoDaRecusa, acaoDeRecusar, enviandoRecusa] = useActionState(recusar, undefined);
  const enviando = aprovando || enviandoRecusa;
  const falha = estadoDaAprovacao?.falha ?? estadoDaRecusa?.falha;

  if (falha && (falha.status === 404 || falha.status === 409)) {
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
      {recusando ? (
        <form action={acaoDeRecusar} className="flex flex-col gap-3">
          <div className="flex flex-col gap-2">
            <Label htmlFor="motivo">Motivo da recusa (opcional)</Label>
            <Textarea
              id="motivo"
              name="motivo"
              rows={4}
              maxLength={tamanhoMaximoDoMotivo}
              aria-invalid={estadoDaRecusa?.erroDoMotivo ? true : undefined}
              aria-describedby={estadoDaRecusa?.erroDoMotivo ? "erro-do-motivo" : "ajuda-do-motivo"}
            />
            {estadoDaRecusa?.erroDoMotivo ? (
              <p id="erro-do-motivo" className="text-sm text-destructive">
                {estadoDaRecusa.erroDoMotivo}
              </p>
            ) : (
              <p id="ajuda-do-motivo" className="text-sm text-muted-foreground">
                Até {tamanhoMaximoDoMotivo} caracteres. A oficina refaz o diagnóstico e envia uma nova versão do
                orçamento.
              </p>
            )}
          </div>
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <Button
              type="button"
              variant="outline"
              size="lg"
              disabled={enviando}
              onClick={() => setRecusando(false)}
            >
              Voltar
            </Button>
            <Button type="submit" variant="destructive" size="lg" disabled={enviando}>
              Confirmar recusa
            </Button>
          </div>
        </form>
      ) : (
        <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
          <Button
            type="button"
            variant="outline"
            size="lg"
            disabled={enviando}
            onClick={() => setRecusando(true)}
          >
            Recusar
          </Button>
          <form action={acaoDeAprovar} className="flex flex-col">
            <Button type="submit" size="lg" disabled={enviando}>
              Aprovar orçamento
            </Button>
          </form>
        </div>
      )}
    </div>
  );
}
