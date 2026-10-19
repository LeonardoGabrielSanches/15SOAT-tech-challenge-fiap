import { ClipboardPlus } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import type { ClienteDto, VeiculoDto } from "@/lib/api/dtos";
import { FormularioDeVeiculo } from "./formulario-de-veiculo";
import { InativarVeiculo } from "./inativar-veiculo";
import { TransferirVeiculo } from "./transferir-veiculo";

export function AcoesDoVeiculo({ cliente, veiculo }: { cliente: ClienteDto; veiculo: VeiculoDto }) {
  if (!veiculo.ativo) {
    return null;
  }
  return (
    <div className="flex flex-wrap justify-end gap-2">
      {cliente.ativo && (
        <Button asChild size="sm">
          <Link href={`/administrativo/ordens-de-servico/nova?veiculoId=${veiculo.id}`}>
            <ClipboardPlus />
            Abrir OS
          </Link>
        </Button>
      )}
      <FormularioDeVeiculo clienteId={cliente.id} veiculo={veiculo} />
      <TransferirVeiculo clienteId={cliente.id} veiculo={veiculo} />
      <InativarVeiculo clienteId={cliente.id} veiculo={veiculo} />
    </div>
  );
}
