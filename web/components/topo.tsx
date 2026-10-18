import { LogOut, Wrench } from "lucide-react";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { sair } from "@/lib/autenticacao/acoes";
import type { Funcionario } from "@/lib/autenticacao/sessao";
import { nomeDoPapel } from "@/lib/capacidades";

export function Topo({ funcionario }: { funcionario: Funcionario }) {
  return (
    <header className="flex h-14 items-center justify-between gap-4 border-b bg-background px-4">
      <Link href="/administrativo" className="flex items-center gap-2 font-semibold">
        <Wrench className="size-5" />
        Oficina
      </Link>
      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2 text-sm">
          <span className="hidden font-medium sm:inline">{funcionario.nome}</span>
          <Badge variant="secondary">{funcionario.papel ? nomeDoPapel[funcionario.papel] : "Sem papel"}</Badge>
        </div>
        <form action={sair}>
          <Button type="submit" variant="outline" size="sm">
            <LogOut />
            Sair
          </Button>
        </form>
      </div>
    </header>
  );
}
