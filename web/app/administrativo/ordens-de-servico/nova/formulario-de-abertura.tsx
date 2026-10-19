"use client";

import { ClipboardPlus, ExternalLink } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition, type FormEvent } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import type { ServicoDto } from "@/lib/api/dtos";
import { formatarDinheiro } from "@/lib/formatacao";
import { enderecoDaOs } from "../endereco-da-os";
import { abrirOs, type ResultadoDaAbertura } from "./acoes";

type Props = {
  clienteId: string;
  veiculoId: string;
  servicos: ServicoDto[];
  enderecoDeVolta: string;
};

export function FormularioDeAbertura({ clienteId, veiculoId, servicos, enderecoDeVolta }: Props) {
  const router = useRouter();
  const [falha, setFalha] = useState<Extract<ResultadoDaAbertura, { ok: false }> | null>(null);
  const [selecionados, setSelecionados] = useState<string[]>([]);
  const [pendente, startTransition] = useTransition();
  const total = servicos.filter((s) => selecionados.includes(s.id)).reduce((soma, s) => soma + s.preco, 0);

  function alternar(id: string, marcado: boolean) {
    setSelecionados((atuais) => (marcado ? [...atuais, id] : atuais.filter((atual) => atual !== id)));
  }

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const dados = new FormData(evento.currentTarget);
    startTransition(async () => {
      const resultado = await abrirOs(clienteId, veiculoId, dados);
      startTransition(() => {
        if (resultado.ok) {
          router.push(`${enderecoDaOs(resultado.dados.id)}?aberta=1`);
        } else {
          setFalha(resultado);
        }
      });
    });
  }

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <fieldset className="flex flex-col gap-2" aria-invalid={falha?.falha.status === 422 || undefined}>
        <legend className="mb-2 text-sm font-medium">Serviços (escolha ao menos um)</legend>
        {servicos.map((servico) => (
          <label
            key={servico.id}
            className="flex cursor-pointer items-start gap-3 rounded-md border p-3 text-sm has-checked:border-primary has-checked:bg-muted/50"
          >
            <input
              type="checkbox"
              name="servicoIds"
              value={servico.id}
              checked={selecionados.includes(servico.id)}
              onChange={(evento) => alternar(servico.id, evento.target.checked)}
              className="mt-0.5 size-4 accent-primary"
            />
            <span className="flex min-w-0 flex-1 flex-col gap-0.5">
              <span className="font-medium">{servico.nome}</span>
              {servico.descricao && <span className="text-muted-foreground">{servico.descricao}</span>}
            </span>
            <span className="tabular-nums">{formatarDinheiro(servico.preco)}</span>
          </label>
        ))}
      </fieldset>

      <p className="text-sm">
        {selecionados.length === 0
          ? "Nenhum Serviço escolhido."
          : `${selecionados.length === 1 ? "1 Serviço escolhido" : `${selecionados.length} Serviços escolhidos`}, total pelo catálogo ${formatarDinheiro(total)}.`}
      </p>

      {falha && (
        <div className="flex flex-col gap-2">
          <AvisoDeFalha falha={falha.falha} />
          {falha.osEmAndamento && (
            <Button asChild variant="outline" size="sm" className="self-start">
              <Link href={enderecoDaOs(falha.osEmAndamento.id)}>
                <ExternalLink />
                Ver a OS {falha.osEmAndamento.numero}
              </Link>
            </Button>
          )}
        </div>
      )}

      <div className="flex flex-wrap gap-2">
        <Button type="submit" disabled={pendente}>
          <ClipboardPlus />
          {pendente ? "Abrindo OS..." : "Abrir OS"}
        </Button>
        <Button asChild variant="outline">
          <Link href={enderecoDeVolta}>Voltar ao Cliente</Link>
        </Button>
      </div>
    </form>
  );
}
