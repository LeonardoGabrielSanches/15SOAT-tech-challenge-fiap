"use client";

import { Check, Link2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

type Situacao = { tipo: "inicial" } | { tipo: "copiado" } | { tipo: "manual"; link: string };

export function CopiarLink({ caminho }: { caminho: string }) {
  const [situacao, setSituacao] = useState<Situacao>({ tipo: "inicial" });

  async function copiar() {
    const link = new URL(caminho, window.location.origin).toString();
    try {
      await navigator.clipboard.writeText(link);
      setSituacao({ tipo: "copiado" });
      setTimeout(() => setSituacao({ tipo: "inicial" }), 2500);
    } catch {
      setSituacao({ tipo: "manual", link });
    }
  }

  return (
    <div className="flex flex-col items-end gap-2">
      <Button type="button" variant="outline" size="sm" onClick={copiar}>
        {situacao.tipo === "copiado" ? <Check /> : <Link2 />}
        {situacao.tipo === "copiado" ? "Link copiado" : "Copiar link"}
      </Button>
      <span className="sr-only" aria-live="polite">
        {situacao.tipo === "copiado" ? "Link da OS copiado." : ""}
      </span>
      {situacao.tipo === "manual" && (
        <div className="flex w-72 max-w-full flex-col gap-1">
          <Input
            readOnly
            value={situacao.link}
            aria-label="Link da OS"
            onFocus={(evento) => evento.currentTarget.select()}
            autoFocus
          />
          <span className="text-xs text-muted-foreground">Não foi possível copiar. Copie o link acima.</span>
        </div>
      )}
    </div>
  );
}
