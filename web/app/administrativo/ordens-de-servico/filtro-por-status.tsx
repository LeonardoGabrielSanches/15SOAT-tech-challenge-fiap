import Link from "next/link";
import { Button } from "@/components/ui/button";
import type { StatusOs } from "@/lib/api/dtos";
import { nomeDoStatus } from "@/lib/formatacao";

export const statusDaOs = Object.keys(nomeDoStatus) as StatusOs[];

export function lerStatusDaOs(valor: string | string[] | undefined): StatusOs | null {
  return typeof valor === "string" && statusDaOs.includes(valor as StatusOs) ? (valor as StatusOs) : null;
}

const opcoes: { status: StatusOs | null; rotulo: string }[] = [
  { status: null, rotulo: "Em andamento" },
  ...statusDaOs.map((status) => ({ status, rotulo: nomeDoStatus[status] })),
];

export function FiltroPorStatus({ statusSelecionado }: { statusSelecionado: StatusOs | null }) {
  return (
    <nav aria-label="Filtrar por Status" className="flex flex-wrap gap-2">
      {opcoes.map(({ status, rotulo }) => {
        const selecionado = status === statusSelecionado;
        return (
          <Button key={rotulo} asChild size="sm" variant={selecionado ? "default" : "outline"}>
            <Link
              href={status ? `/administrativo/ordens-de-servico?status=${status}` : "/administrativo/ordens-de-servico"}
              aria-current={selecionado ? "page" : undefined}
            >
              {rotulo}
            </Link>
          </Button>
        );
      })}
    </nav>
  );
}
