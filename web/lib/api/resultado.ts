import "server-only";
import { redirect } from "next/navigation";
import { enderecoDeEntrada } from "@/lib/autenticacao/sessao";

export type Falha = {
  status: number;
  titulo: string;
  mensagem: string;
  sugestao?: string;
};

export type Resultado<T> = { ok: true; dados: T } | { ok: false; falha: Falha };

type RespostaDaApi = {
  data?: unknown;
  error?: unknown;
  response: Response;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

const padroesPorStatus: Record<number, { titulo: string; mensagem: string; sugestao?: string }> = {
  400: { titulo: "Dados inválidos", mensagem: "A API recusou os dados enviados." },
  403: { titulo: "Acesso negado", mensagem: "O seu papel não permite esta ação." },
  404: { titulo: "Não encontrado", mensagem: "O registro procurado não existe." },
  409: {
    titulo: "Conflito",
    mensagem: "A operação conflita com a situação atual do registro.",
    sugestao: "Recarregue a página para ver a situação atual.",
  },
  422: { titulo: "Dados inválidos", mensagem: "A API recusou os dados enviados." },
};

const falhaDeConexao: Falha = {
  status: 0,
  titulo: "API indisponível",
  mensagem: "Não foi possível falar com a API da oficina. Tente novamente em instantes.",
};

function mensagemDoProblema(erro: unknown): string | undefined {
  if (!erro || typeof erro !== "object") {
    return typeof erro === "string" && erro.trim() ? erro : undefined;
  }
  const problema = erro as ProblemDetails;
  if (problema.detail) {
    return problema.detail;
  }
  const erros = Object.values(problema.errors ?? {}).flat();
  return erros.length > 0 ? erros.join(" ") : undefined;
}

export function dadosInvalidos(
  mensagem: string,
  titulo: string = padroesPorStatus[422].titulo,
): { ok: false; falha: Falha } {
  return { ok: false, falha: { status: 422, titulo, mensagem } };
}

export function falhaDaResposta(status: number, erro: unknown): Falha {
  const padrao = padroesPorStatus[status] ?? {
    titulo: "Falha na API",
    mensagem: "A API não conseguiu concluir a operação.",
  };
  return {
    status,
    titulo: padrao.titulo,
    mensagem: mensagemDoProblema(erro) ?? padrao.mensagem,
    sugestao: padrao.sugestao,
  };
}

export async function resultadoDaApi<T = undefined>(chamada: Promise<RespostaDaApi>): Promise<Resultado<T>> {
  let resposta: RespostaDaApi;
  try {
    resposta = await chamada;
  } catch {
    return { ok: false, falha: falhaDeConexao };
  }
  if (resposta.response.ok) {
    return { ok: true, dados: resposta.data as T };
  }
  if (resposta.response.status === 401) {
    redirect(enderecoDeEntrada());
  }
  return { ok: false, falha: falhaDaResposta(resposta.response.status, resposta.error) };
}
