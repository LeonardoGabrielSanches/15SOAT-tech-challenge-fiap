import { CircleCheck, CircleX } from "lucide-react";
import type { components } from "@/lib/api/openapi";
import type { Falha } from "@/lib/api/resultado";

export type DecisaoDoOrcamento = components["schemas"]["DecisaoDoOrcamento"];

export type DecisaoConfirmada = "aprovado" | "recusado";

export type FalhaDaDecisao = { falha?: Falha; erroDoMotivo?: string };

export const confirmacaoDa: Record<DecisaoDoOrcamento, DecisaoConfirmada> = {
  Aprovar: "aprovado",
  Recusar: "recusado",
};

export const confirmacoes = {
  aprovado: {
    Icone: CircleCheck,
    titulo: "Orçamento aprovado",
    descricao: "Obrigado! A oficina já pode começar o trabalho no seu Veículo.",
  },
  recusado: {
    Icone: CircleX,
    titulo: "Orçamento recusado",
    descricao: "A oficina vai refazer o diagnóstico e enviar uma nova versão do orçamento, com um novo Token de aprovação.",
  },
} satisfies Record<DecisaoConfirmada, unknown>;

export function ehDecisaoDoOrcamento(valor: unknown): valor is DecisaoDoOrcamento {
  return typeof valor === "string" && Object.hasOwn(confirmacaoDa, valor);
}

export function ehDecisaoConfirmada(valor: unknown): valor is DecisaoConfirmada {
  return typeof valor === "string" && Object.hasOwn(confirmacoes, valor);
}

export function impedeNovaDecisao(falha: Falha): boolean {
  return falha.status === 404 || falha.status === 409;
}
