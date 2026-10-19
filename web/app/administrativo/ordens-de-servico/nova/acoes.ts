"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { OrdemDeServicoResumoDto } from "@/lib/api/dtos";
import { dadosInvalidos, resultadoDaApi, type Falha } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";

export type ResultadoDaAbertura =
  | { ok: true; dados: OrdemDeServicoResumoDto }
  | { ok: false; falha: Falha; osEmAndamento?: OrdemDeServicoResumoDto };

type Api = Awaited<ReturnType<typeof apiAdministrativa>>;

async function osEmAndamentoDoVeiculo(api: Api, veiculoId: string): Promise<OrdemDeServicoResumoDto | undefined> {
  const resultado = await resultadoDaApi<OrdemDeServicoResumoDto[]>(api.GET("/api/administrativo/ordens-de-servico"));
  return resultado.ok ? resultado.dados.find((os) => os.veiculoId === veiculoId) : undefined;
}

export async function abrirOs(clienteId: string, veiculoId: string, dados: FormData): Promise<ResultadoDaAbertura> {
  await exigirTela("ordensDeServico");
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
  const osEmAndamento = await osEmAndamentoDoVeiculo(api, veiculoId);
  if (!osEmAndamento) {
    return resultado;
  }
  return {
    ok: false,
    falha: {
      status: 409,
      titulo: "Veículo com OS em andamento",
      mensagem: `Este Veículo já tem a OS ${osEmAndamento.numero} em andamento. Uma nova OS só pode ser aberta depois que ela for entregue ou cancelada.`,
    },
    osEmAndamento,
  };
}
