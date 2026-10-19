"use client";

import { Pencil, Plus } from "lucide-react";
import { useState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
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
import { Textarea } from "@/components/ui/textarea";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { ServicoDto } from "@/lib/api/dtos";
import { formatarDinheiroParaEdicao } from "@/lib/formatacao";
import { salvarServico } from "./acoes";

export function FormularioDeServico({ servico }: { servico?: ServicoDto }) {
  const [aberto, setAberto] = useState(false);

  return (
    <Dialog open={aberto} onOpenChange={setAberto}>
      <DialogTrigger asChild>
        {servico ? (
          <Button variant="outline" size="sm">
            <Pencil />
            Editar
          </Button>
        ) : (
          <Button>
            <Plus />
            Cadastrar Serviço
          </Button>
        )}
      </DialogTrigger>
      <DialogContent>
        <CamposDoServico servico={servico} aoSalvar={() => setAberto(false)} />
      </DialogContent>
    </Dialog>
  );
}

function CamposDoServico({ servico, aoSalvar }: { servico?: ServicoDto; aoSalvar: () => void }) {
  const { falha, enviar, pendente } = useAcaoDeFormulario(salvarServico, () => aoSalvar());

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>{servico ? "Editar Serviço" : "Cadastrar Serviço"}</DialogTitle>
        <DialogDescription>Nome, descrição opcional e preço em reais.</DialogDescription>
      </DialogHeader>
      {servico && <input type="hidden" name="id" value={servico.id} />}
      <div className="flex flex-col gap-2">
        <Label htmlFor="nome">Nome</Label>
        <Input id="nome" name="nome" defaultValue={servico?.nome} required maxLength={200} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="descricao">Descrição</Label>
        <Textarea id="descricao" name="descricao" defaultValue={servico?.descricao ?? ""} maxLength={1000} />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="preco">Preço (R$)</Label>
        <Input
          id="preco"
          name="preco"
          inputMode="decimal"
          placeholder="0,00"
          defaultValue={servico ? formatarDinheiroParaEdicao(servico.preco) : ""}
          required
        />
      </div>
      {falha && <AvisoDeFalha falha={falha} />}
      <DialogFooter>
        <DialogClose asChild>
          <Button type="button" variant="outline">
            Cancelar
          </Button>
        </DialogClose>
        <Button type="submit" disabled={pendente}>
          {pendente ? "Salvando..." : "Salvar"}
        </Button>
      </DialogFooter>
    </form>
  );
}
