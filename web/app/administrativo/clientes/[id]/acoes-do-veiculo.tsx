import { ClipboardPlus } from "lucide-react";
import Link from "next/link";
import { InativarComConfirmacao } from "@/components/inativar-com-confirmacao";
import { Button } from "@/components/ui/button";
import type { ClienteDto, VeiculoDto } from "@/lib/api/dtos";
import { inativarVeiculo } from "./acoes-de-veiculo";
import { FormularioDeVeiculo } from "./formulario-de-veiculo";
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
      <InativarComConfirmacao
        titulo={`Inativar o Veículo ${veiculo.placa}?`}
        descricao={`O Veículo ${veiculo.marca} ${veiculo.modelo} não poderá ser usado em novas operações, como abrir uma OS ou ser transferido. O histórico de OS continua preservado. Não há como reativá-lo depois.`}
        acao={inativarVeiculo.bind(null, cliente.id, veiculo.id)}
      />
    </div>
  );
}
