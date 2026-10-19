"use client";

import { ArrowRight } from "lucide-react";
import { useRouter } from "next/navigation";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import { enderecoDaOs } from "../ordens-de-servico/endereco-da-os";
import { localizarOs } from "./acoes";

export function FormularioDeLocalizacao() {
  const router = useRouter();
  const { resultado, falha, enviar, pendente } = useAcaoDeFormulario(localizarOs, ({ id }) => router.push(enderecoDaOs(id)));

  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <div className="flex flex-col gap-2">
        <Label htmlFor="referencia">Link ou identificador da OS</Label>
        <Input
          id="referencia"
          name="referencia"
          required
          autoFocus
          autoComplete="off"
          spellCheck={false}
          placeholder="Link copiado ou identificador da OS"
          aria-describedby="referencia-ajuda"
          aria-invalid={falha !== null && falha.status !== 0}
        />
        <p id="referencia-ajuda" className="text-xs text-muted-foreground">
          O Atendente copia o link no botão &quot;Copiar link&quot; da página da OS. O identificador é a parte final do link.
        </p>
      </div>
      {falha && <AvisoDeFalha falha={falha} />}
      <Button type="submit" className="self-start" disabled={pendente || resultado?.ok === true}>
        <ArrowRight />
        {pendente || resultado?.ok ? "Abrindo..." : "Abrir OS"}
      </Button>
    </form>
  );
}
