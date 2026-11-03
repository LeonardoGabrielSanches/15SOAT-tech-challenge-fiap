"use client";

import { ClipboardPlus } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { ServicoDto } from "@/lib/api/dtos";
import { formatarDinheiro } from "@/lib/formatacao";
import { enderecoDaOs } from "../endereco-da-os";
import { abrirOs } from "./acoes";

type Props = {
  clienteId: string;
  veiculoId: string;
  servicos: ServicoDto[];
  enderecoDeVolta: string;
};

export function FormularioDeAbertura({ clienteId, veiculoId, servicos, enderecoDeVolta }: Props) {
  const router = useRouter();
  const [selecionados, setSelecionados] = useState<string[]>([]);
  const { falha, dadosInvalidos, enviar, pendente } = useAcaoDeFormulario(
    (dados) => abrirOs(clienteId, veiculoId, dados),
    (os) => router.push(`${enderecoDaOs(os.id)}?aberta=1`),
  );
  const total = servicos.filter((s) => selecionados.includes(s.id)).reduce((soma, s) => soma + s.preco, 0);

  function alternar(id: string, marcado: boolean) {
    setSelecionados((atuais) => (marcado ? [...atuais, id] : atuais.filter((atual) => atual !== id)));
  }

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <fieldset className="flex flex-col gap-2" aria-invalid={dadosInvalidos || undefined}>
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

      {falha && <AvisoDeFalha falha={falha} />}

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
