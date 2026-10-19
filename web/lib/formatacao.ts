import type { SituacaoDaVersao, StatusOs } from "@/lib/api/dtos";
import type { TipoPecaInsumo } from "@/lib/api/dtos";

const fusoHorario = "America/Sao_Paulo";

const dinheiro = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

const dataHora = new Intl.DateTimeFormat("pt-BR", {
  timeZone: fusoHorario,
  dateStyle: "short",
  timeStyle: "short",
});

const data = new Intl.DateTimeFormat("pt-BR", { timeZone: fusoHorario, dateStyle: "short" });

const quantidade = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 3 });

export const nomeDoStatus: Record<StatusOs, string> = {
  Recebida: "Recebida",
  EmDiagnostico: "Em diagnóstico",
  AguardandoAprovacao: "Aguardando aprovação",
  Aprovada: "Aprovada",
  EmExecucao: "Em execução",
  Finalizada: "Finalizada",
  Entregue: "Entregue",
  Cancelada: "Cancelada",
};

export const nomeDaSituacao: Record<SituacaoDaVersao, string> = {
  Pendente: "Pendente",
  Aprovada: "Aprovada",
  Recusada: "Recusada",
  Superada: "Superada",
};

export function formatarDinheiro(valor: number): string {
  return dinheiro.format(valor);
}

export function formatarDataHora(valor: string): string {
  return dataHora.format(new Date(valor));
}

export function formatarData(valor: string): string {
  return data.format(new Date(valor));
}

export function formatarQuantidade(valor: number): string {
  return quantidade.format(valor);
}

export const nomeDoTipoPecaInsumo: Record<TipoPecaInsumo, string> = {
  Peca: "Peça",
  Insumo: "Insumo",
};

export function formatarSaldo(valor: number, unidadeDeMedida: string): string {
  return `${formatarQuantidade(valor)} ${unidadeDeMedida}`;
}

export function formatarParaCampoDecimal(valor: number): string {
  return String(valor).replace(".", ",");
}

export function lerNumeroDigitado(texto: string): number | null {
  const limpo = texto.trim().replace(/\s/g, "");
  const normalizado = limpo.includes(",") ? limpo.replace(/\./g, "").replace(",", ".") : limpo;
  return /^-?\d+(\.\d+)?$/.test(normalizado) ? Number(normalizado) : null;
}
