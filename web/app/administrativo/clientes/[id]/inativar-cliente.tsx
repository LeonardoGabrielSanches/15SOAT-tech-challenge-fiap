"use client";

import { Ban } from "lucide-react";
import { useActionState, useState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { inativarCliente } from "../acoes";

export function InativarCliente({ id, nome }: { id: string; nome: string }) {
  const [aberto, setAberto] = useState(false);

  return (
    <AlertDialog open={aberto} onOpenChange={setAberto}>
      <AlertDialogTrigger asChild>
        <Button variant="destructive">
          <Ban />
          Inativar Cliente
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <ConfirmacaoDaInativacao id={id} nome={nome} aoInativar={() => setAberto(false)} />
      </AlertDialogContent>
    </AlertDialog>
  );
}

function ConfirmacaoDaInativacao({ id, nome, aoInativar }: { id: string; nome: string; aoInativar: () => void }) {
  const [falha, acao, enviando] = useActionState(async () => {
    const resultado = await inativarCliente(id);
    if (!resultado) {
      aoInativar();
    }
    return resultado;
  }, null);

  return (
    <form action={acao} className="flex flex-col gap-4">
      <AlertDialogHeader>
        <AlertDialogTitle>Inativar o Cliente {nome}?</AlertDialogTitle>
        <AlertDialogDescription>
          O Cliente não poderá ser usado em novas operações, mas continua visível no histórico. Não há como reativá-lo
          depois.
        </AlertDialogDescription>
      </AlertDialogHeader>
      {falha && <AvisoDeFalha falha={falha} />}
      <AlertDialogFooter>
        <AlertDialogCancel type="button">Cancelar</AlertDialogCancel>
        <Button type="submit" variant="destructive" disabled={enviando}>
          {enviando ? "Inativando..." : "Inativar"}
        </Button>
      </AlertDialogFooter>
    </form>
  );
}
