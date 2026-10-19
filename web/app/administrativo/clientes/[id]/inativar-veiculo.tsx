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
import type { VeiculoDto } from "@/lib/api/dtos";
import type { Resultado } from "@/lib/api/resultado";
import { inativarVeiculo } from "./acoes-de-veiculo";

type Props = { clienteId: string; veiculo: VeiculoDto };

export function InativarVeiculo({ clienteId, veiculo }: Props) {
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
        <ConfirmacaoDaInativacao clienteId={clienteId} veiculo={veiculo} aoInativar={() => setAberto(false)} />
      </AlertDialogContent>
    </AlertDialog>
  );
}

function ConfirmacaoDaInativacao({ clienteId, veiculo, aoInativar }: Props & { aoInativar: () => void }) {
  const [estado, acao, enviando] = useActionState(async (): Promise<Resultado<undefined> | null> => {
    const resultado = await inativarVeiculo(clienteId, veiculo.id);
    if (resultado.ok) {
      aoInativar();
    }
    return resultado;
  }, null);
  const falha = estado && !estado.ok ? estado.falha : null;

  return (
    <form action={acao} className="flex flex-col gap-4">
      <AlertDialogHeader>
        <AlertDialogTitle>Inativar o Veículo {veiculo.placa}?</AlertDialogTitle>
        <AlertDialogDescription>
          O Veículo {veiculo.marca} {veiculo.modelo} não poderá ser usado em novas operações, como abrir uma OS ou ser
          transferido. O histórico de OS continua preservado. Não há como reativá-lo depois.
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
