import "server-only";
import { apiPublica } from "@/lib/api/cliente";
import { resultadoDaApi, type Falha } from "@/lib/api/resultado";
import { lerTexto } from "@/lib/dados-do-formulario";
import type { components } from "@/lib/api/openapi";
import { ehDecisaoDoOrcamento, type DecisaoDoOrcamento, type FalhaDaDecisao } from "./decisao-do-cliente";

type DecisaoDoOrcamentoRequest = components["schemas"]["DecisaoDoOrcamentoRequest"];

export type DesfechoDaDecisao = { ok: true; decisao: DecisaoDoOrcamento } | { ok: false; falha: FalhaDaDecisao };

const decisaoInvalida: Falha = {
  status: 400,
  titulo: "Decisão inválida",
  mensagem: "Escolha Aprovar ou Recusar o orçamento.",
};

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

function lerPedidoDeDecisao(formData: FormData): DecisaoDoOrcamentoRequest | undefined {
  const decisao = lerTexto(formData, "decisao");
  if (!ehDecisaoDoOrcamento(decisao)) {
    return undefined;
  }
  return decisao === "Recusar" ? { decisao, motivo: lerTexto(formData, "motivo") || null } : { decisao };
}

export async function decidirOrcamento(token: string, formData: FormData): Promise<DesfechoDaDecisao> {
  const pedido = lerPedidoDeDecisao(formData);
  if (!pedido) {
    return { ok: false, falha: { falha: decisaoInvalida } };
  }
  const resultado = await resultadoDaApi(
    apiPublica().POST("/api/publico/orcamentos/{token}/decisao", {
      params: { path: { token } },
      body: pedido,
    }),
  );
  if (resultado.ok) {
    return { ok: true, decisao: pedido.decisao };
  }
  if (resultado.falha.status === 422) {
    return { ok: false, falha: { erroDoMotivo: resultado.falha.mensagem } };
  }
  return { ok: false, falha: { falha: falhaDaDecisao(resultado.falha) } };
}
