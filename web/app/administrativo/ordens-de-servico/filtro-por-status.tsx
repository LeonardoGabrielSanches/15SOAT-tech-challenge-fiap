import Link from "next/link";
import { Button } from "@/components/ui/button";
import type { StatusOs } from "@/lib/api/dtos";
import { nomeDoStatus } from "@/lib/formatacao";

export const statusDaOs = Object.keys(nomeDoStatus) as StatusOs[];

export function lerStatusDaOs(valor: string | string[] | undefined): StatusOs | null {
  return typeof valor === "string" && statusDaOs.includes(valor as StatusOs) ? (valor as StatusOs) : null;
}

export function enderecoDaListagem(status: StatusOs | null, pagina = 1): string {
  const consulta = new URLSearchParams();
  if (status) consulta.set("status", status);
  if (pagina > 1) consulta.set("pagina", String(pagina));
  const texto = consulta.toString();
  return texto ? `/administrativo/ordens-de-servico?${texto}` : "/administrativo/ordens-de-servico";
}

const opcoes: { status: StatusOs | null; rotulo: string }[] = [
  { status: null, rotulo: "Pendentes" },
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
              href={enderecoDaListagem(status)}
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
