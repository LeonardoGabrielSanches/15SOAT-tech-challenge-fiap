"use client";

import { useState, useTransition, type FormEvent } from "react";
import type { Resultado } from "@/lib/api/resultado";

type Sucesso<R> = Extract<R, { ok: true }>;

type Fracasso<R> = Extract<R, { ok: false }>;

export function useAcaoDeFormulario<R extends Resultado<unknown>>(
  acao: (dados: FormData) => Promise<R>,
  aoConcluir?: (dados: Sucesso<R>["dados"], formulario: HTMLFormElement) => void,
) {
  const [resultado, setResultado] = useState<R | null>(null);
  const [pendente, startTransition] = useTransition();
  const fracasso = resultado && !resultado.ok ? (resultado as Fracasso<R>) : null;
  const falha = fracasso ? fracasso.falha : null;

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const formulario = evento.currentTarget;
    const dados = new FormData(formulario);
    startTransition(async () => {
      const novo = await acao(dados);
      startTransition(() => {
        setResultado(novo);
        if (novo.ok) {
          aoConcluir?.((novo as Sucesso<R>).dados, formulario);
        }
      });
    });
  }

  function limpar() {
    setResultado(null);
  }

  return { resultado, fracasso, falha, dadosInvalidos: falha?.status === 422, enviar, pendente, limpar };
}
