"use client";

import { X } from "lucide-react";
import { useState } from "react";
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
import type { ServicoDaOsDto } from "@/lib/api/dtos";
import { removerServicoDaOs } from "./acoes-do-atendente";
import { FalhaDaAcao } from "./falha-da-acao";
import type { PropsDaAcaoNoServico } from "./tipos";

export function RemoverServico({ os, servico }: PropsDaAcaoNoServico) {
  const [aberto, setAberto] = useState(false);

  if (os.servicos.length <= 1) {
    return (
      <Button variant="outline" size="sm" disabled title="A OS deve manter ao menos um Serviço.">
        <X />
        Remover
        <span className="sr-only"> (a OS deve manter ao menos um Serviço)</span>
      </Button>
    );
  }

  return (
    <AlertDialog open={aberto} onOpenChange={setAberto}>
      <AlertDialogTrigger asChild>
        <Button variant="outline" size="sm">
          <X />
          Remover
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <ConfirmacaoDaRemocao osId={os.id} servico={servico} aoRemover={() => setAberto(false)} />
      </AlertDialogContent>
    </AlertDialog>
  );
}

function ConfirmacaoDaRemocao({
  osId,
  servico,
  aoRemover,
}: {
  osId: string;
  servico: ServicoDaOsDto;
  aoRemover: () => void;
}) {
  const { falha, enviar, pendente } = useAcaoDeFormulario(() => removerServicoDaOs(osId, servico.servicoId), aoRemover);

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <AlertDialogHeader>
        <AlertDialogTitle>Remover o Serviço {servico.nome} da OS?</AlertDialogTitle>
        <AlertDialogDescription>
          O Serviço sai da OS. Ele pode ser incluído de novo enquanto o Status permitir.
        </AlertDialogDescription>
      </AlertDialogHeader>
      {falha && <FalhaDaAcao falha={falha} />}
      <AlertDialogFooter>
        <AlertDialogCancel type="button">Voltar</AlertDialogCancel>
        <Button type="submit" variant="destructive" disabled={pendente}>
          {pendente ? "Removendo..." : "Remover"}
        </Button>
      </AlertDialogFooter>
    </form>
  );
}
