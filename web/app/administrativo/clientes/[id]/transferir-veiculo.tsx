"use client";

import { ArrowLeft, ArrowRightLeft, Search } from "lucide-react";
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
import type { ClienteDto, VeiculoDto } from "@/lib/api/dtos";
import { formatarDocumento, nomeDoTipoDeDocumento } from "@/lib/formatacao";
import { buscarNovoProprietario, transferirVeiculo } from "./acoes-de-veiculo";

type Props = { clienteId: string; veiculo: VeiculoDto };

export function TransferirVeiculo({ clienteId, veiculo }: Props) {
  const [aberto, setAberto] = useState(false);

  return (
    <Dialog open={aberto} onOpenChange={setAberto}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <ArrowRightLeft />
          Transferir
        </Button>
      </DialogTrigger>
      <DialogContent>
        <Transferencia clienteId={clienteId} veiculo={veiculo} aoTransferir={() => setAberto(false)} />
      </DialogContent>
    </Dialog>
  );
}

function Transferencia({ clienteId, veiculo, aoTransferir }: Props & { aoTransferir: () => void }) {
  const [documento, setDocumento] = useState("");
  const [novoProprietario, setNovoProprietario] = useState<ClienteDto | null>(null);

  return (
    <div className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>Transferir o Veículo {veiculo.placa}</DialogTitle>
        <DialogDescription>
          Informe o CPF ou CNPJ do novo Proprietário atual. O histórico de OS do Veículo é preservado.
        </DialogDescription>
      </DialogHeader>
      {novoProprietario ? (
        <ConfirmacaoDaTransferencia
          clienteId={clienteId}
          veiculo={veiculo}
          novoProprietario={novoProprietario}
          aoVoltar={() => setNovoProprietario(null)}
          aoTransferir={aoTransferir}
        />
      ) : (
        <BuscaDoNovoProprietario
          clienteId={clienteId}
          documento={documento}
          aoMudarDocumento={setDocumento}
          aoEncontrar={setNovoProprietario}
        />
      )}
    </div>
  );
}

function BuscaDoNovoProprietario({
  clienteId,
  documento,
  aoMudarDocumento,
  aoEncontrar,
}: {
  clienteId: string;
  documento: string;
  aoMudarDocumento: (documento: string) => void;
  aoEncontrar: (cliente: ClienteDto) => void;
}) {
  const { falha, enviar, pendente } = useAcaoDeFormulario(
    (dados) => buscarNovoProprietario(clienteId, dados),
    (cliente) => aoEncontrar(cliente),
  );

  return (
    <form onSubmit={enviar} noValidate className="flex flex-col gap-4">
      <div className="flex flex-col gap-2">
        <Label htmlFor="transferencia-documento">CPF ou CNPJ do novo Proprietário atual</Label>
        <Input
          id="transferencia-documento"
          name="documento"
          value={documento}
          onChange={(evento) => aoMudarDocumento(evento.target.value)}
          autoComplete="off"
          autoFocus
          placeholder="000.000.000-00 ou 00.000.000/0000-00"
          aria-invalid={falha ? true : undefined}
        />
        <p className="text-xs text-muted-foreground">Com ou sem pontuação.</p>
      </div>
      {falha && <AvisoDeFalha falha={falha} />}
      <DialogFooter>
        <DialogClose asChild>
          <Button type="button" variant="outline">
            Cancelar
          </Button>
        </DialogClose>
        <Button type="submit" disabled={pendente}>
          <Search />
          {pendente ? "Buscando..." : "Buscar Cliente"}
        </Button>
      </DialogFooter>
    </form>
  );
}

function ConfirmacaoDaTransferencia({
  clienteId,
  veiculo,
  novoProprietario,
  aoVoltar,
  aoTransferir,
}: Props & { novoProprietario: ClienteDto; aoVoltar: () => void; aoTransferir: () => void }) {
  const { falha, enviar, pendente } = useAcaoDeFormulario(
    () => transferirVeiculo(veiculo.id, clienteId, novoProprietario.id),
    aoTransferir,
  );

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1 rounded-lg border p-3">
        <span className="text-xs text-muted-foreground">Novo Proprietário atual</span>
        <span className="font-medium break-words">{novoProprietario.nome}</span>
        <span className="text-sm text-muted-foreground">
          {nomeDoTipoDeDocumento[novoProprietario.tipoDocumento]}{" "}
          {formatarDocumento(novoProprietario.tipoDocumento, novoProprietario.documento)}
        </span>
      </div>
      <p className="text-sm">
        O Veículo {veiculo.placa} ({veiculo.marca} {veiculo.modelo}) deixa de aparecer neste Cliente e passa para{" "}
        {novoProprietario.nome}.
      </p>
      {falha && <AvisoDeFalha falha={falha} />}
      <DialogFooter>
        <Button type="button" variant="outline" onClick={aoVoltar} disabled={pendente}>
          <ArrowLeft />
          Voltar
        </Button>
        <Button type="submit" disabled={pendente}>
          <ArrowRightLeft />
          {pendente ? "Transferindo..." : "Transferir"}
        </Button>
      </DialogFooter>
    </form>
  );
}
