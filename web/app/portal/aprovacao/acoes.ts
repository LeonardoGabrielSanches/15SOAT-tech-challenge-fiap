"use server";

import { redirect } from "next/navigation";
import { apiPublica } from "@/lib/api/cliente";
import { resultadoDaApi, type Falha, type Resultado } from "@/lib/api/resultado";
import { lerTexto } from "@/lib/dados-do-formulario";
import type { components } from "@/lib/api/openapi";

type DecisaoDoOrcamentoRequest = components["schemas"]["DecisaoDoOrcamentoRequest"];

export type EstadoDoToken = { erro?: string } | undefined;

export type EstadoDaDecisao = { falha?: Falha; erroDoMotivo?: string } | undefined;

function falhaDaDecisao(falha: Falha): Falha {
  if (falha.status === 404) {
    return {
      status: 404,
      titulo: "Token de aprovação não encontrado",
      mensagem:
        "Não encontramos um orçamento com este Token de aprovação. Confira se o token foi copiado inteiro, sem espaços e com as letras maiúsculas.",
    };
  }
  if (falha.status === 409) {
    return {
      status: 409,
      titulo: "Este orçamento não pode mais ser decidido",
      mensagem:
        "O Token de aprovação já foi usado, foi substituído por uma versão mais nova do orçamento ou a OS foi cancelada.",
      sugestao: "Se você recebeu um orçamento mais recente, use o token dele. Em caso de dúvida, fale com a oficina.",
    };
  }
  return falha;
}

function estadoDaFalha(resultado: Resultado<undefined>): EstadoDaDecisao {
  if (resultado.ok) {
    return undefined;
  }
  if (resultado.falha.status === 422) {
    return { erroDoMotivo: resultado.falha.mensagem };
  }
  return { falha: falhaDaDecisao(resultado.falha) };
}

export async function irParaAprovacao(_estado: EstadoDoToken, formData: FormData): Promise<EstadoDoToken> {
  const token = lerTexto(formData, "token").replace(/\s+/g, "");
  if (!token) {
    return { erro: "Informe o Token de aprovação." };
  }
  redirect(`/portal/aprovacao/${encodeURIComponent(token)}`);
}

function decidirOrcamento(token: string, corpo: DecisaoDoOrcamentoRequest) {
  return resultadoDaApi(
    apiPublica().POST("/api/publico/orcamentos/{token}/decisao", {
      params: { path: { token } },
      body: corpo,
    }),
  );
}

export async function aprovarOrcamento(token: string): Promise<EstadoDaDecisao> {
  const resultado = await decidirOrcamento(token, { decisao: "Aprovar" });
  if (!resultado.ok) {
    return estadoDaFalha(resultado);
  }
  redirect("/portal/aprovacao/concluida?decisao=aprovado");
}

export async function recusarOrcamento(
  token: string,
  _estado: EstadoDaDecisao,
  formData: FormData,
): Promise<EstadoDaDecisao> {
  const motivo = lerTexto(formData, "motivo");
  const resultado = await decidirOrcamento(token, { decisao: "Recusar", motivo: motivo || null });
  if (!resultado.ok) {
    return estadoDaFalha(resultado);
  }
  redirect("/portal/aprovacao/concluida?decisao=recusado");
}
