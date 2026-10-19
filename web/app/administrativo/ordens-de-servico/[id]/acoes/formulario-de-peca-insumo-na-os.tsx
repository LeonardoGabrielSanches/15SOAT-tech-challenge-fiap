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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { PecaInsumoDto } from "@/lib/api/dtos";
import type { Falha } from "@/lib/api/resultado";
import { formatarSaldo, nomeDoTipoPecaInsumo } from "@/lib/formatacao";
import { incluirPecaInsumo } from "./acoes-do-diagnostico";
import { FalhaDaAcao } from "./falha-da-acao";

type Props = {
  osId: string;
  opcoes: PecaInsumoDto[];
  falhaAoCarregar: Falha | null;
  quantidadesNaOs: Record<string, number>;
};

const classeDoSelect =
  "h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:opacity-60";

export function FormularioDePecaInsumoNaOs(props: Props) {
  const [aberto, setAberto] = useState(false);

  return (
    <Dialog open={aberto} onOpenChange={setAberto}>
      <DialogTrigger asChild>
        <Button size="sm">
          <Plus />
          Incluir Peça/insumo
        </Button>
      </DialogTrigger>
      <DialogContent>
        <CamposDaPecaInsumo {...props} aoIncluir={() => setAberto(false)} />
      </DialogContent>
    </Dialog>
  );
}

function CamposDaPecaInsumo({
  osId,
  opcoes,
  falhaAoCarregar,
  quantidadesNaOs,
  aoIncluir,
}: Props & { aoIncluir: () => void }) {
  const [selecionadaId, setSelecionadaId] = useState("");
  const { falha, dadosInvalidos, enviar, pendente, limpar } = useAcaoDeFormulario(
    (dados) => incluirPecaInsumo(osId, dados),
    aoIncluir,
  );
  const selecionada = opcoes.find((opcao) => opcao.id === selecionadaId);
  const naOs = selecionada ? quantidadesNaOs[selecionada.id] : undefined;
  const pecas = opcoes.filter((opcao) => opcao.tipo === "Peca");
  const insumos = opcoes.filter((opcao) => opcao.tipo === "Insumo");

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <DialogHeader>
        <DialogTitle>Incluir Peça/insumo na OS</DialogTitle>
        <DialogDescription>
          A quantidade é baixada do estoque na hora. Se a Peça/insumo já estiver na OS, a quantidade é somada.
        </DialogDescription>
      </DialogHeader>
      {falhaAoCarregar ? (
        <AvisoDeFalha falha={falhaAoCarregar} />
      ) : opcoes.length === 0 ? (
        <p className="text-sm text-muted-foreground">Nenhuma Peça/insumo ativa no catálogo.</p>
      ) : (
        <>
          <div className="flex flex-col gap-2">
            <Label htmlFor="pecaInsumoId">Peça/insumo</Label>
            <select
              id="pecaInsumoId"
              name="pecaInsumoId"
              required
              value={selecionadaId}
              onChange={(evento) => {
                setSelecionadaId(evento.target.value);
                limpar();
              }}
              className={classeDoSelect}
            >
              <option value="" disabled>
                Escolha uma Peça/insumo
              </option>
              {[
                { rotulo: "Peças", itens: pecas },
                { rotulo: "Insumos", itens: insumos },
              ]
                .filter((grupo) => grupo.itens.length > 0)
                .map((grupo) => (
                  <optgroup key={grupo.rotulo} label={grupo.rotulo}>
                    {grupo.itens.map((opcao) => (
                      <option key={opcao.id} value={opcao.id} disabled={opcao.saldo <= 0}>
                        {opcao.nome} — saldo {formatarSaldo(opcao.saldo, opcao.unidadeDeMedida)}
                        {opcao.saldo <= 0 ? " (sem saldo)" : ""}
                      </option>
                    ))}
                  </optgroup>
                ))}
            </select>
            {selecionada && (
              <p className="text-sm text-muted-foreground">
                {nomeDoTipoPecaInsumo[selecionada.tipo]} · Saldo atual:{" "}
                <span className="font-medium text-foreground tabular-nums">
                  {formatarSaldo(selecionada.saldo, selecionada.unidadeDeMedida)}
                </span>
                {naOs !== undefined && <> · Já na OS: {formatarSaldo(naOs, selecionada.unidadeDeMedida)}</>}
              </p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="quantidade">
              Quantidade{selecionada ? ` (${selecionada.unidadeDeMedida})` : ""}
            </Label>
            <Input
              id="quantidade"
              name="quantidade"
              required
              autoComplete="off"
              inputMode={selecionada?.tipo === "Insumo" ? "decimal" : "numeric"}
              placeholder={selecionada?.tipo === "Insumo" ? "3,5" : "1"}
              aria-describedby="quantidade-ajuda"
              aria-invalid={dadosInvalidos || undefined}
            />
            <p id="quantidade-ajuda" className="text-xs text-muted-foreground">
              {selecionada?.tipo === "Insumo"
                ? "Insumo aceita frações, com até 3 casas decimais (ex.: 3,5)."
                : selecionada?.tipo === "Peca"
                  ? "Peça aceita só quantidade inteira."
                  : "Inteiro para Peça; frações (até 3 casas decimais) para Insumo."}
            </p>
          </div>
          {falha && <FalhaDaAcao falha={falha} />}
        </>
      )}
      <DialogFooter>
        <DialogClose asChild>
          <Button type="button" variant="outline">
            Cancelar
          </Button>
        </DialogClose>
        {!falhaAoCarregar && opcoes.length > 0 && (
          <Button type="submit" disabled={pendente}>
            {pendente ? "Incluindo..." : "Incluir"}
          </Button>
        )}
      </DialogFooter>
    </form>
  );
}
