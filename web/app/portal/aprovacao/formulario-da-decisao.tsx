"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";

const tamanhoMaximoDoMotivo = 500;

type Props = {
  acao: (formData: FormData) => void;
  enviando: boolean;
  erroDoMotivo?: string;
};

export function FormularioDaDecisao({ acao, enviando, erroDoMotivo }: Props) {
  const [recusando, setRecusando] = useState(false);

  if (recusando) {
    return (
      <form action={acao} className="flex flex-col gap-3">
        <input type="hidden" name="decisao" value="Recusar" />
        <div className="flex flex-col gap-2">
          <Label htmlFor="motivo">Motivo da recusa (opcional)</Label>
          <Textarea
            id="motivo"
            name="motivo"
            rows={4}
            maxLength={tamanhoMaximoDoMotivo}
            aria-invalid={erroDoMotivo ? true : undefined}
            aria-describedby={erroDoMotivo ? "erro-do-motivo" : "ajuda-do-motivo"}
          />
          {erroDoMotivo ? (
            <p id="erro-do-motivo" className="text-sm text-destructive">
              {erroDoMotivo}
            </p>
          ) : (
            <p id="ajuda-do-motivo" className="text-sm text-muted-foreground">
              Até {tamanhoMaximoDoMotivo} caracteres. A oficina refaz o diagnóstico e envia uma nova versão do
              orçamento.
            </p>
          )}
        </div>
        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button type="button" variant="outline" size="lg" disabled={enviando} onClick={() => setRecusando(false)}>
            Voltar
          </Button>
          <Button type="submit" variant="destructive" size="lg" disabled={enviando}>
            Confirmar recusa
          </Button>
        </div>
      </form>
    );
  }

  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
      <Button type="button" variant="outline" size="lg" disabled={enviando} onClick={() => setRecusando(true)}>
        Recusar
      </Button>
      <form action={acao} className="flex flex-col">
        <input type="hidden" name="decisao" value="Aprovar" />
        <Button type="submit" size="lg" disabled={enviando}>
          Aprovar orçamento
        </Button>
      </form>
    </div>
  );
}
