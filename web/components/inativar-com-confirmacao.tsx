"use client";

import { Ban } from "lucide-react";
import { useState } from "react";
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
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { Resultado } from "@/lib/api/resultado";

type Confirmacao = {
  titulo: string;
  descricao: string;
  acao: () => Promise<Resultado<undefined>>;
};

type Props = Confirmacao & {
  rotulo?: string;
  tamanho?: "sm" | "default";
};

export function InativarComConfirmacao({ rotulo = "Inativar", tamanho = "sm", ...confirmacao }: Props) {
  const [aberto, setAberto] = useState(false);

  return (
    <AlertDialog open={aberto} onOpenChange={setAberto}>
      <AlertDialogTrigger asChild>
        <Button variant="destructive" size={tamanho}>
          <Ban />
          {rotulo}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <ConfirmacaoDaInativacao {...confirmacao} aoInativar={() => setAberto(false)} />
      </AlertDialogContent>
    </AlertDialog>
  );
}

function ConfirmacaoDaInativacao({ titulo, descricao, acao, aoInativar }: Confirmacao & { aoInativar: () => void }) {
  const { falha, enviar, pendente } = useAcaoDeFormulario(() => acao(), aoInativar);

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <AlertDialogHeader>
        <AlertDialogTitle>{titulo}</AlertDialogTitle>
        <AlertDialogDescription>{descricao}</AlertDialogDescription>
      </AlertDialogHeader>
      {falha && <AvisoDeFalha falha={falha} />}
      <AlertDialogFooter>
        <AlertDialogCancel type="button">Cancelar</AlertDialogCancel>
        <Button type="submit" variant="destructive" disabled={pendente}>
          {pendente ? "Inativando..." : "Inativar"}
        </Button>
      </AlertDialogFooter>
    </form>
  );
}
