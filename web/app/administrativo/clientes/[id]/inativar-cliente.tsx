"use client";

import { useActionState, useState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { inativarCliente } from "../acoes";

export function InativarCliente({ id, nome }: { id: string; nome: string }) {
  const [confirmando, setConfirmando] = useState(false);
  const [falha, inativar, inativando] = useActionState(() => inativarCliente(id), null);

  return (
    <div className="flex flex-col gap-3">
      {falha && <AvisoDeFalha falha={falha} />}
      {confirmando ? (
        <form action={inativar} className="flex flex-col gap-3">
          <p className="text-sm">
            Inativar <strong>{nome}</strong>? O Cliente não poderá ser usado em novas operações, mas continua visível no
            histórico. Não há como reativá-lo depois.
          </p>
          <div className="flex flex-wrap gap-2">
            <Button type="submit" variant="destructive" disabled={inativando}>
              {inativando ? "Inativando..." : "Confirmar inativação"}
            </Button>
            <Button type="button" variant="outline" disabled={inativando} onClick={() => setConfirmando(false)}>
              Voltar
            </Button>
          </div>
        </form>
      ) : (
        <div>
          <Button type="button" variant="destructive" onClick={() => setConfirmando(true)}>
            Inativar Cliente
          </Button>
        </div>
      )}
    </div>
  );
}
