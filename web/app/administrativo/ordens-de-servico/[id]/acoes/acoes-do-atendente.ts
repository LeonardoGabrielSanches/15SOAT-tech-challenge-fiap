"use server";

import type { Resultado } from "@/lib/api/resultado";
import { lerTexto } from "@/lib/dados-do-formulario";
import { executarNaOs } from "./executar-na-os";

function dadosInvalidos(mensagem: string): Resultado<undefined> {
  return { ok: false, falha: { status: 422, titulo: "Dados inválidos", mensagem } };
}

export async function incluirServicoNaOs(osId: string, dados: FormData): Promise<Resultado<undefined>> {
  const servicoId = lerTexto(dados, "servicoId");
  if (!servicoId) {
    return dadosInvalidos("Escolha o Serviço a incluir.");
  }
  return executarNaOs(osId, (api) =>
    api.POST("/api/administrativo/ordens-de-servico/{id}/servicos", {
      params: { path: { id: osId } },
      body: { servicoId },
    }),
  );
}

export async function removerServicoDaOs(osId: string, servicoId: string): Promise<Resultado<undefined>> {
  return executarNaOs(osId, (api) =>
    api.DELETE("/api/administrativo/ordens-de-servico/{id}/servicos/{servicoId}", {
      params: { path: { id: osId, servicoId } },
    }),
  );
}

export async function cancelarOs(osId: string, dados: FormData): Promise<Resultado<undefined>> {
  const motivo = lerTexto(dados, "motivo");
  if (!motivo) {
    return dadosInvalidos("Informe o motivo do cancelamento.");
  }
  return executarNaOs(osId, (api) =>
    api.POST("/api/administrativo/ordens-de-servico/{id}/cancelar", {
      params: { path: { id: osId } },
      body: { motivo },
    }),
  );
}

export async function registrarEntrega(osId: string): Promise<Resultado<undefined>> {
  return executarNaOs(osId, (api) =>
    api.POST("/api/administrativo/ordens-de-servico/{id}/entrega", { params: { path: { id: osId } } }),
  );
}
