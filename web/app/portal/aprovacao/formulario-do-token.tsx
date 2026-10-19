"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { irParaAprovacao } from "./acoes";

export function FormularioDoToken() {
  const [estado, acao, enviando] = useActionState(irParaAprovacao, undefined);
  return (
    <form action={acao} className="flex flex-col gap-3">
      <div className="flex flex-col gap-2">
        <Label htmlFor="token">Token de aprovação</Label>
        <Input
          id="token"
          name="token"
          autoComplete="off"
          autoCapitalize="characters"
          spellCheck={false}
          className="font-mono"
          aria-invalid={estado?.erro ? true : undefined}
          aria-describedby={estado?.erro ? "erro-do-token" : undefined}
        />
        {estado?.erro && (
          <p id="erro-do-token" className="text-sm text-destructive">
            {estado.erro}
          </p>
        )}
      </div>
      <Button type="submit" size="lg" className="w-full sm:w-auto sm:self-end" disabled={enviando}>
        Continuar
      </Button>
    </form>
  );
}
