"use server";

import type { Resultado } from "@/lib/api/resultado";
import { lerQuantidade } from "@/lib/formatacao";
import { executarNaOs } from "./executar-na-os";

function dadosInvalidos(mensagem: string): Resultado<undefined> {
  return { ok: false, falha: { status: 422, titulo: "Dados inválidos", mensagem } };
}

export async function iniciarDiagnostico(osId: string): Promise<Resultado<undefined>> {
  return executarNaOs(osId, (api) =>
    api.POST("/api/administrativo/ordens-de-servico/{id}/diagnostico/iniciar", { params: { path: { id: osId } } }),
  );
}

export async function concluirDiagnostico(osId: string): Promise<Resultado<undefined>> {
  return executarNaOs(osId, (api) =>
    api.POST("/api/administrativo/ordens-de-servico/{id}/diagnostico/concluir", { params: { path: { id: osId } } }),
  );
}

export async function incluirPecaInsumo(osId: string, dados: FormData): Promise<Resultado<undefined>> {
  const pecaInsumoId = String(dados.get("pecaInsumoId") ?? "");
  if (!pecaInsumoId) {
    return dadosInvalidos("Escolha a Peça/insumo.");
  }
  const quantidade = lerQuantidade(String(dados.get("quantidade") ?? ""));
  if (quantidade === null) {
    return dadosInvalidos("Informe a quantidade como número, por exemplo 2 ou 3,5.");
  }
  return executarNaOs(osId, (api) =>
    api.POST("/api/administrativo/ordens-de-servico/{id}/pecas-insumos", {
      params: { path: { id: osId } },
      body: { pecaInsumoId, quantidade },
    }),
  );
}

export async function removerPecaInsumo(
  osId: string,
  pecaInsumoId: string,
  dados: FormData,
): Promise<Resultado<undefined>> {
  const caminho = { id: osId, pecaInsumoId };
  if (dados.get("modo") !== "parte") {
    return executarNaOs(osId, (api) =>
      api.DELETE("/api/administrativo/ordens-de-servico/{id}/pecas-insumos/{pecaInsumoId}", {
        params: { path: caminho },
      }),
    );
  }
  const quantidade = lerQuantidade(String(dados.get("quantidade") ?? ""));
  if (quantidade === null) {
    return dadosInvalidos("Informe a quantidade a remover como número, por exemplo 1 ou 0,5.");
  }
  return executarNaOs(osId, (api) =>
    api.DELETE("/api/administrativo/ordens-de-servico/{id}/pecas-insumos/{pecaInsumoId}", {
      params: { path: caminho, query: { quantidade } },
    }),
  );
}
