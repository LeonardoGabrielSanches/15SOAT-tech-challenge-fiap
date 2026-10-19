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
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { VeiculoDto } from "@/lib/api/dtos";
import type { Falha } from "@/lib/api/resultado";
import { cadastrarVeiculo, editarVeiculo } from "./acoes-de-veiculo";

type CampoDoVeiculo = "placa" | "marca" | "modelo" | "anoFabricacao";

function campoDaFalha(falha: Falha | null): CampoDoVeiculo | null {
  if (!falha) {
    return null;
  }
  const mensagem = falha.mensagem.toLowerCase();
  if (mensagem.includes("placa")) {
    return "placa";
  }
  if (mensagem.includes("ano de fabricacao")) {
    return "anoFabricacao";
  }
  if (mensagem.includes("marca")) {
    return "marca";
  }
  if (mensagem.includes("modelo")) {
    return "modelo";
  }
  return null;
}

type Props = { clienteId: string; veiculo?: VeiculoDto };

export function FormularioDeVeiculo({ clienteId, veiculo }: Props) {
  const [aberto, setAberto] = useState(false);

  return (
    <Dialog open={aberto} onOpenChange={setAberto}>
      <DialogTrigger asChild>
        {veiculo ? (
          <Button variant="outline" size="sm">
            <Pencil />
            Editar
          </Button>
        ) : (
          <Button size="sm">
            <Plus />
            Cadastrar Veículo
          </Button>
        )}
      </DialogTrigger>
      <DialogContent>
        <CamposDoVeiculo clienteId={clienteId} veiculo={veiculo} aoSalvar={() => setAberto(false)} />
      </DialogContent>
    </Dialog>
  );
}

function CamposDoVeiculo({ clienteId, veiculo, aoSalvar }: Props & { aoSalvar: () => void }) {
  const { resultado, enviar, pendente } = useAcaoDeFormulario(
    (dados) => (veiculo ? editarVeiculo(clienteId, veiculo.id, dados) : cadastrarVeiculo(clienteId, dados)),
    aoSalvar,
  );
  const falha = resultado && !resultado.ok ? resultado.falha : null;
  const campoInvalido = campoDaFalha(falha);
  const invalido = (campo: CampoDoVeiculo) => campoInvalido === campo || undefined;

  return (
    <form onSubmit={enviar} noValidate className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>{veiculo ? `Editar Veículo ${veiculo.placa}` : "Cadastrar Veículo"}</DialogTitle>
        <DialogDescription>
          {veiculo
            ? "A placa não pode ser alterada. Corrija marca, modelo ou ano de fabricação."
            : "Placa no formato antigo (ABC1234) ou Mercosul (BRA2E19), com ou sem hífen."}
        </DialogDescription>
      </DialogHeader>
      {!veiculo && (
        <div className="flex flex-col gap-2">
          <Label htmlFor="veiculo-placa">Placa</Label>
          <Input
            id="veiculo-placa"
            name="placa"
            autoComplete="off"
            maxLength={8}
            placeholder="BRA2E19"
            className="uppercase"
            aria-invalid={invalido("placa")}
          />
        </div>
      )}
      <div className="flex flex-col gap-2">
        <Label htmlFor="veiculo-marca">Marca</Label>
        <Input
          id="veiculo-marca"
          name="marca"
          autoComplete="off"
          maxLength={100}
          defaultValue={veiculo?.marca}
          aria-invalid={invalido("marca")}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="veiculo-modelo">Modelo</Label>
        <Input
          id="veiculo-modelo"
          name="modelo"
          autoComplete="off"
          maxLength={100}
          defaultValue={veiculo?.modelo}
          aria-invalid={invalido("modelo")}
        />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="veiculo-ano">Ano de fabricação</Label>
        <Input
          id="veiculo-ano"
          name="anoFabricacao"
          inputMode="numeric"
          autoComplete="off"
          maxLength={4}
          placeholder="2021"
          defaultValue={veiculo?.anoFabricacao}
          aria-invalid={invalido("anoFabricacao")}
          className="sm:max-w-32"
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
          {pendente ? "Salvando..." : veiculo ? "Salvar" : "Cadastrar Veículo"}
        </Button>
      </DialogFooter>
    </form>
  );
}
