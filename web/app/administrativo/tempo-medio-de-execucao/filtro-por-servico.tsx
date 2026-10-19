"use client";

import { usePathname, useRouter } from "next/navigation";
import { useTransition } from "react";

export type OpcaoDeServico = {
  id: string;
  nome: string;
};

export function FiltroPorServico({
  opcoes,
  servicoSelecionado,
}: {
  opcoes: OpcaoDeServico[];
  servicoSelecionado: string | null;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const [atualizando, iniciarAtualizacao] = useTransition();

  function selecionar(servicoId: string) {
    const destino = servicoId ? `${pathname}?servicoId=${encodeURIComponent(servicoId)}` : pathname;
    iniciarAtualizacao(() => router.replace(destino));
  }

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor="servicoId" className="text-sm font-medium">
        Serviço
      </label>
      <select
        id="servicoId"
        value={servicoSelecionado ?? ""}
        onChange={(evento) => selecionar(evento.target.value)}
        disabled={atualizando}
        aria-busy={atualizando}
        className="h-9 w-full max-w-sm rounded-md border border-input bg-background px-3 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:opacity-60"
      >
        <option value="">Todos</option>
        {opcoes.map((opcao) => (
          <option key={opcao.id} value={opcao.id}>
            {opcao.nome}
          </option>
        ))}
      </select>
    </div>
  );
}
