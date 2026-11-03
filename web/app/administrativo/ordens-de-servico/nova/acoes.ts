"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { OrdemDeServicoResumoDto } from "@/lib/api/dtos";
import { dadosInvalidos, resultadoDaApi, type Falha } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";

export type ResultadoDaAbertura = { ok: true; dados: OrdemDeServicoResumoDto } | { ok: false; falha: Falha };

export async function abrirOs(clienteId: string, veiculoId: string, dados: FormData): Promise<ResultadoDaAbertura> {
  await exigirTela("clientes");
  const servicoIds = dados.getAll("servicoIds").filter((valor): valor is string => typeof valor === "string");
  if (servicoIds.length === 0) {
    return dadosInvalidos("Escolha ao menos um Serviço para abrir a OS.");
  }

  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<OrdemDeServicoResumoDto>(
    api.POST("/api/administrativo/ordens-de-servico", { body: { clienteId, veiculoId, servicoIds } }),
  );
  if (resultado.ok) {
    revalidatePath("/administrativo/ordens-de-servico");
    return resultado;
  }

  const { falha } = resultado;
  if (falha.status !== 409 || falha.mensagem.toLowerCase().includes("inativo")) {
    return resultado;
  }
  return {
    ok: false,
    falha: {
      status: 409,
      titulo: "Veículo com OS em andamento",
      mensagem:
        "Este Veículo já tem uma OS em andamento. Uma nova OS só pode ser aberta depois que ela for entregue ou cancelada.",
    },
  };
}
