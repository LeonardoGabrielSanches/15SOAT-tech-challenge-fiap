"use client";

import { useState, useTransition, type FormEvent } from "react";
import type { Resultado } from "@/lib/api/resultado";

export function useAcaoDeFormulario<T>(
  acao: (dados: FormData) => Promise<Resultado<T>>,
  aoConcluir?: (dados: T, formulario: HTMLFormElement) => void,
) {
  const [resultado, setResultado] = useState<Resultado<T> | null>(null);
  const [pendente, startTransition] = useTransition();

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const formulario = evento.currentTarget;
    const dados = new FormData(formulario);
    startTransition(async () => {
      const novo = await acao(dados);
      startTransition(() => {
        setResultado(novo);
        if (novo.ok) {
          aoConcluir?.(novo.dados, formulario);
        }
      });
    });
  }

  function limpar() {
    setResultado(null);
  }

  return { resultado, enviar, pendente, limpar };
}
