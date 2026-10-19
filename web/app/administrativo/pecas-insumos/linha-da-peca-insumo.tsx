"use client";

import { useState } from "react";
import { AtivoOuInativo } from "@/components/ativo-ou-inativo";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { InativarComConfirmacao } from "@/components/inativar-com-confirmacao";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TableCell, TableRow } from "@/components/ui/table";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { PecaInsumoDto } from "@/lib/api/dtos";
import {
  formatarDinheiro,
  formatarDinheiroParaEdicao,
  formatarSaldo,
  nomeDoTipoPecaInsumo,
} from "@/lib/formatacao";
import { editarPecaInsumo, inativarPecaInsumo, registrarEntradaDeMercadoria } from "./acoes";

type Painel = "entrada" | "edicao";

type Props = {
  item: PecaInsumoDto;
  mostrarSituacao: boolean;
  podeEscrever: boolean;
};

export function LinhaDaPecaInsumo({ item, mostrarSituacao, podeEscrever }: Props) {
  const [painel, setPainel] = useState<Painel | null>(null);
  const colunas = 5 + (mostrarSituacao ? 1 : 0) + (podeEscrever ? 1 : 0);
  const fechar = () => setPainel(null);

  return (
    <>
      <TableRow className={item.ativo ? undefined : "text-muted-foreground"}>
        <TableCell className="font-medium">{item.nome}</TableCell>
        <TableCell>{nomeDoTipoPecaInsumo[item.tipo]}</TableCell>
        <TableCell>{item.unidadeDeMedida}</TableCell>
        <TableCell className="text-right tabular-nums">{formatarDinheiro(item.preco)}</TableCell>
        <TableCell className="text-right tabular-nums">{formatarSaldo(item.saldo, item.unidadeDeMedida)}</TableCell>
        {mostrarSituacao && (
          <TableCell>
            <AtivoOuInativo ativo={item.ativo} />
          </TableCell>
        )}
        {podeEscrever && (
          <TableCell>
            <div className="flex flex-wrap justify-end gap-1">
              {item.ativo && (
                <Button
                  size="sm"
                  variant={painel === "entrada" ? "secondary" : "outline"}
                  onClick={() => setPainel(painel === "entrada" ? null : "entrada")}
                >
                  Entrada de mercadoria
                </Button>
              )}
              <Button
                size="sm"
                variant={painel === "edicao" ? "secondary" : "outline"}
                onClick={() => setPainel(painel === "edicao" ? null : "edicao")}
              >
                Editar
              </Button>
              {item.ativo && (
                <InativarComConfirmacao
                  titulo={`Inativar a Peça/insumo ${item.nome}?`}
                  descricao="Ela sai da lista padrão e deixa de aceitar Entrada de mercadoria e inclusão em OS. O saldo atual é mantido."
                  acao={() => inativarPecaInsumo(item.id)}
                />
              )}
            </div>
          </TableCell>
        )}
      </TableRow>
      {painel && (
        <TableRow className="bg-muted/30 hover:bg-muted/30">
          <TableCell colSpan={colunas} className="whitespace-normal">
            {painel === "entrada" && <EntradaDeMercadoria item={item} aoFechar={fechar} />}
            {painel === "edicao" && <EdicaoDaPecaInsumo item={item} aoFechar={fechar} />}
          </TableCell>
        </TableRow>
      )}
    </>
  );
}

type PropsDoPainel = {
  item: PecaInsumoDto;
  aoFechar: () => void;
};

function EntradaDeMercadoria({ item, aoFechar }: PropsDoPainel) {
  const { resultado, enviar, pendente } = useAcaoDeFormulario(
    (dados) => registrarEntradaDeMercadoria(item.id, dados),
    (_, formulario) => formulario.reset(),
  );
  const campo = `entrada-${item.id}`;
  const regra =
    item.tipo === "Peca" ? "Peça aceita apenas quantidade inteira." : "Insumo aceita até 3 casas decimais.";

  return (
    <form onSubmit={enviar} className="flex max-w-xl flex-col gap-3 py-2">
      <p className="font-medium">Entrada de mercadoria: {item.nome}</p>
      <div className="flex flex-col gap-2">
        <Label htmlFor={campo}>Quantidade ({item.unidadeDeMedida})</Label>
        <Input
          id={campo}
          name="quantidade"
          required
          inputMode={item.tipo === "Peca" ? "numeric" : "decimal"}
          className="max-w-40"
          autoFocus
        />
        <p className="text-xs text-muted-foreground">{regra}</p>
      </div>
      {resultado && !resultado.ok && <AvisoDeFalha falha={resultado.falha} />}
      {resultado?.ok && (
        <p className="text-sm" role="status">
          Entrada registrada. Saldo atual: {formatarSaldo(resultado.dados.saldo, resultado.dados.unidadeDeMedida)}.
        </p>
      )}
      <div className="flex gap-2">
        <Button type="submit" size="sm" disabled={pendente}>
          {pendente ? "Registrando..." : "Registrar entrada"}
        </Button>
        <Button type="button" size="sm" variant="ghost" onClick={aoFechar}>
          Fechar
        </Button>
      </div>
    </form>
  );
}

function EdicaoDaPecaInsumo({ item, aoFechar }: PropsDoPainel) {
  const { resultado, enviar, pendente } = useAcaoDeFormulario(
    (dados) => editarPecaInsumo(item.id, dados),
    () => aoFechar(),
  );

  return (
    <form onSubmit={enviar} className="flex max-w-3xl flex-col gap-3 py-2">
      <p className="font-medium">
        Editar {item.nome}{" "}
        <span className="font-normal text-muted-foreground">
          (tipo {nomeDoTipoPecaInsumo[item.tipo]}, não pode ser alterado)
        </span>
      </p>
      <div className="grid gap-3 sm:grid-cols-3">
        <div className="flex flex-col gap-2">
          <Label htmlFor={`edicao-nome-${item.id}`}>Nome</Label>
          <Input id={`edicao-nome-${item.id}`} name="nome" required maxLength={200} defaultValue={item.nome} />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor={`edicao-unidade-${item.id}`}>Unidade de medida</Label>
          <Input
            id={`edicao-unidade-${item.id}`}
            name="unidadeDeMedida"
            required
            maxLength={30}
            defaultValue={item.unidadeDeMedida}
          />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor={`edicao-preco-${item.id}`}>Preço (R$)</Label>
          <Input
            id={`edicao-preco-${item.id}`}
            name="preco"
            required
            inputMode="decimal"
            defaultValue={formatarDinheiroParaEdicao(item.preco)}
          />
        </div>
      </div>
      {resultado && !resultado.ok && <AvisoDeFalha falha={resultado.falha} />}
      <div className="flex gap-2">
        <Button type="submit" size="sm" disabled={pendente}>
          {pendente ? "Salvando..." : "Salvar"}
        </Button>
        <Button type="button" size="sm" variant="ghost" onClick={aoFechar}>
          Cancelar
        </Button>
      </div>
    </form>
  );
}
