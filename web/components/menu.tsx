"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { Tela } from "@/lib/capacidades";
import { cn } from "@/lib/utils";

export function Menu({ telas }: { telas: Tela[] }) {
  const caminho = usePathname();
  return (
    <nav className="flex gap-1 overflow-x-auto md:flex-col">
      {telas.map((tela) => {
        const ativa = caminho === tela.href || caminho.startsWith(`${tela.href}/`);
        return (
          <Link
            key={tela.id}
            href={tela.href}
            className={cn(
              "rounded-md px-3 py-2 text-sm whitespace-nowrap transition-colors hover:bg-muted",
              ativa && "bg-muted font-medium",
            )}
          >
            {tela.titulo}
          </Link>
        );
      })}
    </nav>
  );
}
