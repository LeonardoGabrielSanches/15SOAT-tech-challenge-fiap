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
      <FormularioDeVeiculo clienteId={cliente.id} veiculo={veiculo} />
      <TransferirVeiculo clienteId={cliente.id} veiculo={veiculo} />
      <InativarVeiculo clienteId={cliente.id} veiculo={veiculo} />
    </div>
  );
}
