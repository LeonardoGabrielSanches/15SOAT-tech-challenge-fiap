"use client";

import { Ban } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { OrdemDeServicoDetalheDto } from "@/lib/api/dtos";
import { cancelarOs } from "./acoes-do-atendente";
import { FalhaDaAcao } from "./falha-da-acao";
import type { PropsDaAcaoDaOs } from "./tipos";

export function CancelarOs({ os }: PropsDaAcaoDaOs) {
  const [aberto, setAberto] = useState(false);

  return (
    <Dialog open={aberto} onOpenChange={setAberto}>
      <DialogTrigger asChild>
        <Button variant="destructive">
          <Ban />
          Cancelar OS
        </Button>
      </DialogTrigger>
      <DialogContent>
        <FormularioDeCancelamento os={os} aoCancelar={() => setAberto(false)} />
      </DialogContent>
    </Dialog>
  );
}

function FormularioDeCancelamento({ os, aoCancelar }: { os: OrdemDeServicoDetalheDto; aoCancelar: () => void }) {
  const [motivo, setMotivo] = useState("");
  const { resultado, enviar, pendente } = useAcaoDeFormulario((dados) => cancelarOs(os.id, dados), aoCancelar);
  const motivoEmBranco = motivo.trim().length === 0;
  const temPecasInsumos = os.pecasInsumos.length > 0;

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>Cancelar a OS {os.numero}?</DialogTitle>
        <DialogDescription>
          O cancelamento encerra a OS e não pode ser desfeito.
          {temPecasInsumos && " As Peças/insumos incluídas voltam ao estoque."}
          {os.status === "AguardandoAprovacao" && " O link de aprovação enviado ao Cliente deixa de valer."}
        </DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-2">
        <Label htmlFor="motivo">Motivo do cancelamento</Label>
        <Textarea
          id="motivo"
          name="motivo"
          required
          value={motivo}
          onChange={(evento) => setMotivo(evento.target.value)}
          aria-invalid={(resultado && !resultado.ok && resultado.falha.status === 422) || undefined}
          placeholder="Ex.: Cliente desistiu do serviço"
        />
      </div>
      {resultado && !resultado.ok && <FalhaDaAcao falha={resultado.falha} />}
      <DialogFooter>
        <DialogClose asChild>
          <Button type="button" variant="outline">
            Voltar
          </Button>
        </DialogClose>
        <Button type="submit" variant="destructive" disabled={pendente || motivoEmBranco}>
          {pendente ? "Cancelando..." : "Cancelar OS"}
        </Button>
      </DialogFooter>
    </form>
  );
}
