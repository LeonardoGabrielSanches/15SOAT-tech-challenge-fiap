"use client";

import { Minus } from "lucide-react";
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { PecaInsumoDaOsDto } from "@/lib/api/dtos";
import { formatarSaldo } from "@/lib/formatacao";
import { removerPecaInsumo } from "./acoes-do-diagnostico";
import { FalhaDaAcao } from "./falha-da-acao";
import type { PropsDaAcaoNaPecaInsumo } from "./tipos";

type Modo = "tudo" | "parte";

export function RemoverPecaInsumo({ os, pecaInsumo }: PropsDaAcaoNaPecaInsumo) {
  const [aberto, setAberto] = useState(false);

  return (
    <Dialog open={aberto} onOpenChange={setAberto}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" aria-label={`Remover ${pecaInsumo.nome}`}>
          <Minus />
          Remover
        </Button>
      </DialogTrigger>
      <DialogContent>
        <CamposDaRemocao osId={os.id} pecaInsumo={pecaInsumo} aoRemover={() => setAberto(false)} />
      </DialogContent>
    </Dialog>
  );
}

function CamposDaRemocao({
  osId,
  pecaInsumo,
  aoRemover,
}: {
  osId: string;
  pecaInsumo: PecaInsumoDaOsDto;
  aoRemover: () => void;
}) {
  const [modo, setModo] = useState<Modo>("tudo");
  const { falha, dadosInvalidos, enviar, pendente, limpar } = useAcaoDeFormulario(
    (dados) => removerPecaInsumo(osId, pecaInsumo.pecaInsumoId, dados),
    aoRemover,
  );
  const naOs = formatarSaldo(pecaInsumo.quantidade, pecaInsumo.unidadeDeMedida);

  function escolher(novo: Modo) {
    setModo(novo);
    limpar();
  }

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>Remover {pecaInsumo.nome} da OS</DialogTitle>
        <DialogDescription>
          Na OS: {naOs}. O Estorno devolve ao estoque a quantidade removida.
        </DialogDescription>
      </DialogHeader>
      <fieldset className="flex flex-col gap-3">
        <legend className="sr-only">Quanto remover</legend>
        <Label className="font-normal">
          <input
            type="radio"
            name="modo"
            value="tudo"
            checked={modo === "tudo"}
            onChange={() => escolher("tudo")}
            className="size-4 accent-primary"
          />
          Toda a quantidade ({naOs})
        </Label>
        <Label className="font-normal">
          <input
            type="radio"
            name="modo"
            value="parte"
            checked={modo === "parte"}
            onChange={() => escolher("parte")}
            className="size-4 accent-primary"
          />
          Só uma parte
        </Label>
      </fieldset>
      {modo === "parte" && (
        <div className="flex flex-col gap-2">
          <Label htmlFor="quantidade-a-remover">Quantidade a remover ({pecaInsumo.unidadeDeMedida})</Label>
          <Input
            id="quantidade-a-remover"
            name="quantidade"
            required
            autoFocus
            autoComplete="off"
            inputMode="decimal"
            placeholder="1"
            aria-describedby="quantidade-a-remover-ajuda"
            aria-invalid={dadosInvalidos || undefined}
          />
          <p id="quantidade-a-remover-ajuda" className="text-xs text-muted-foreground">
            Até {naOs}. Inteiro para Peça; frações (até 3 casas decimais) para Insumo.
          </p>
        </div>
      )}
      {falha && <FalhaDaAcao falha={falha} />}
      <DialogFooter>
        <DialogClose asChild>
          <Button type="button" variant="outline">
            Cancelar
          </Button>
        </DialogClose>
        <Button type="submit" variant="destructive" disabled={pendente}>
          {pendente ? "Removendo..." : modo === "tudo" ? "Remover tudo" : "Remover quantidade"}
        </Button>
      </DialogFooter>
    </form>
  );
}
