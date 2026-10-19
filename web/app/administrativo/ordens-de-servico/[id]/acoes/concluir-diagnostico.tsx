"use client";

import { ClipboardCheck } from "lucide-react";
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
import type { OrdemDeServicoDetalheDto } from "@/lib/api/dtos";
import { concluirDiagnostico } from "./acoes-do-diagnostico";
import { FalhaDaAcao } from "./falha-da-acao";
import type { PropsDaAcaoDaOs } from "./tipos";

export function ConcluirDiagnostico({ os }: PropsDaAcaoDaOs) {
  const [aberto, setAberto] = useState(false);

  return (
    <AlertDialog open={aberto} onOpenChange={setAberto}>
      <AlertDialogTrigger asChild>
        <Button type="button">
          <ClipboardCheck />
          Concluir diagnóstico
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <ConfirmacaoDaConclusao os={os} aoConcluir={() => setAberto(false)} />
      </AlertDialogContent>
    </AlertDialog>
  );
}

function quantos(quantidade: number, singular: string, plural: string): string {
  return `${quantidade} ${quantidade === 1 ? singular : plural}`;
}

function ConfirmacaoDaConclusao({ os, aoConcluir }: { os: OrdemDeServicoDetalheDto; aoConcluir: () => void }) {
  const { falha, enviar, pendente } = useAcaoDeFormulario(() => concluirDiagnostico(os.id), aoConcluir);
  const proximaVersao = Math.max(0, ...os.versoesDoOrcamento.map((versao) => versao.numero)) + 1;

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <AlertDialogHeader>
        <AlertDialogTitle>Concluir o diagnóstico?</AlertDialogTitle>
        <AlertDialogDescription>
          Será gerada a Versão {proximaVersao} do orçamento com {quantos(os.servicos.length, "Serviço", "Serviços")} e{" "}
          {quantos(os.pecasInsumos.length, "Peça/insumo", "Peças/insumos")}. A OS passa a Aguardando aprovação e o
          Cliente recebe o orçamento para aprovar ou recusar.
        </AlertDialogDescription>
      </AlertDialogHeader>
      {falha && <FalhaDaAcao falha={falha} />}
      <AlertDialogFooter>
        <AlertDialogCancel type="button">Voltar</AlertDialogCancel>
        <Button type="submit" disabled={pendente}>
          {pendente ? "Concluindo..." : "Concluir diagnóstico"}
        </Button>
      </AlertDialogFooter>
    </form>
  );
}
