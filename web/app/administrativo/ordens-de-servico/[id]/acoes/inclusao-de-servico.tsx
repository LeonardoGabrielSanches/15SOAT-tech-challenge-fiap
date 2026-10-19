"use client";

import { Plus } from "lucide-react";
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
import { Label } from "@/components/ui/label";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { ServicoDto } from "@/lib/api/dtos";
import type { Falha } from "@/lib/api/resultado";
import { formatarDinheiro } from "@/lib/formatacao";
import { incluirServicoNaOs } from "./acoes-do-atendente";
import { FalhaDaAcao } from "./falha-da-acao";

type Props = {
  osId: string;
  servicosDisponiveis: ServicoDto[];
  falhaAoCarregar?: Falha;
};

export function InclusaoDeServico({ osId, servicosDisponiveis, falhaAoCarregar }: Props) {
  const [aberto, setAberto] = useState(false);

  return (
    <Dialog open={aberto} onOpenChange={setAberto}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <Plus />
          Incluir Serviço
        </Button>
      </DialogTrigger>
      <DialogContent>
        <FormularioDeInclusao
          osId={osId}
          servicosDisponiveis={servicosDisponiveis}
          falhaAoCarregar={falhaAoCarregar}
          aoIncluir={() => setAberto(false)}
        />
      </DialogContent>
    </Dialog>
  );
}

function FormularioDeInclusao({ osId, servicosDisponiveis, falhaAoCarregar, aoIncluir }: Props & { aoIncluir: () => void }) {
  const { resultado, enviar, pendente } = useAcaoDeFormulario((dados) => incluirServicoNaOs(osId, dados), aoIncluir);
  const semOpcoes = !falhaAoCarregar && servicosDisponiveis.length === 0;

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>Incluir Serviço na OS</DialogTitle>
        <DialogDescription>Só aparecem Serviços ativos do catálogo que ainda não estão na OS.</DialogDescription>
      </DialogHeader>
      {falhaAoCarregar ? (
        <AvisoDeFalha falha={falhaAoCarregar} />
      ) : semOpcoes ? (
        <p className="text-sm text-muted-foreground">Todos os Serviços ativos do catálogo já estão nesta OS.</p>
      ) : (
        <div className="flex flex-col gap-2">
          <Label htmlFor="servicoId">Serviço</Label>
          <select
            id="servicoId"
            name="servicoId"
            required
            defaultValue=""
            aria-invalid={(resultado && !resultado.ok && resultado.falha.status === 422) || undefined}
            className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:opacity-60 aria-invalid:border-destructive"
          >
            <option value="" disabled>
              Escolha um Serviço
            </option>
            {servicosDisponiveis.map((servico) => (
              <option key={servico.id} value={servico.id}>
                {servico.nome} · {formatarDinheiro(servico.preco)}
              </option>
            ))}
          </select>
        </div>
      )}
      {resultado && !resultado.ok && <FalhaDaAcao falha={resultado.falha} />}
      <DialogFooter>
        <DialogClose asChild>
          <Button type="button" variant="outline">
            Voltar
          </Button>
        </DialogClose>
        <Button type="submit" disabled={pendente || !!falhaAoCarregar || semOpcoes}>
          {pendente ? "Incluindo..." : "Incluir"}
        </Button>
      </DialogFooter>
    </form>
  );
}
