"use server";

import { revalidatePath } from "next/cache";
import type { Resultado } from "@/lib/api/resultado";
import { tela } from "@/lib/capacidades";
import { executarNaOs } from "./executar-na-os";

export async function iniciarExecucao(osId: string): Promise<Resultado<undefined>> {
  const resultado = await executarNaOs(osId, (api) =>
    api.POST("/api/administrativo/ordens-de-servico/{id}/execucao/iniciar", { params: { path: { id: osId } } }),
  );
  if (resultado.ok) {
    revalidatePath(tela("filaDeExecucao").href);
  }
  return resultado;
}

export async function finalizarExecucao(osId: string): Promise<Resultado<undefined>> {
  return executarNaOs(osId, (api) =>
    api.POST("/api/administrativo/ordens-de-servico/{id}/execucao/finalizar", { params: { path: { id: osId } } }),
  );
}
