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
import type { ServicoDto } from "@/lib/api/dtos";
import { inativarServico, type EstadoDaInativacao } from "./acoes";

export function InativarServico({ servico }: { servico: ServicoDto }) {
  const [aberto, setAberto] = useState(false);

  return (
    <AlertDialog open={aberto} onOpenChange={setAberto}>
      <AlertDialogTrigger asChild>
        <Button variant="destructive" size="sm">
          <Ban />
          Inativar
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <ConfirmacaoDaInativacao servico={servico} aoInativar={() => setAberto(false)} />
      </AlertDialogContent>
    </AlertDialog>
  );
}

function ConfirmacaoDaInativacao({ servico, aoInativar }: { servico: ServicoDto; aoInativar: () => void }) {
  const [estado, acao, enviando] = useActionState(async (): Promise<EstadoDaInativacao> => {
    const resultado = await inativarServico(servico.id);
    if (resultado?.ok) {
      aoInativar();
    }
    return resultado;
  }, null);
  const falha = estado && !estado.ok ? estado.falha : null;

  return (
    <form action={acao} className="flex flex-col gap-4">
      <AlertDialogHeader>
        <AlertDialogTitle>Inativar o Serviço {servico.nome}?</AlertDialogTitle>
        <AlertDialogDescription>
          O Serviço deixa de ser oferecido em novas OS e continua nas OS em que já foi usado.
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
