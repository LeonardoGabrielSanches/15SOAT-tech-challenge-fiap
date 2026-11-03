"use server";

import { redirect } from "next/navigation";
import { lerTexto } from "@/lib/dados-do-formulario";
import { decidirOrcamento } from "./decisao";
import { confirmacaoDa, type FalhaDaDecisao } from "./decisao-do-cliente";

export type EstadoDoToken = { erro?: string } | undefined;

export type EstadoDaDecisao = FalhaDaDecisao | undefined;

export async function irParaAprovacao(_estado: EstadoDoToken, formData: FormData): Promise<EstadoDoToken> {
  const token = lerTexto(formData, "token").replace(/\s+/g, "");
  if (!token) {
    return { erro: "Informe o Token de aprovação." };
  }
  redirect(`/portal/aprovacao/${encodeURIComponent(token)}`);
}

export async function decidirOrcamentoPeloToken(
  token: string,
  _estado: EstadoDaDecisao,
  formData: FormData,
): Promise<EstadoDaDecisao> {
  const desfecho = await decidirOrcamento(token, formData);
  if (!desfecho.ok) {
    return desfecho.falha;
  }
  redirect(`/portal/aprovacao/concluida?decisao=${confirmacaoDa[desfecho.decisao]}`);
}
