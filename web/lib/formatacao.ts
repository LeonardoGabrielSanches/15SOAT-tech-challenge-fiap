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

export function formatarDuracao(segundos: number): string {
  const minutosTotais = Math.round(segundos / 60);
  if (minutosTotais < 1) {
    return "menos de 1 min";
  }
  const horas = Math.floor(minutosTotais / 60);
  const minutos = minutosTotais % 60;
  if (horas === 0) {
    return `${minutos} min`;
  }
  const textoDasHoras = `${formatarQuantidade(horas)} h`;
  return minutos === 0 ? textoDasHoras : `${textoDasHoras} ${minutos} min`;
}

const dinheiroParaEdicao = new Intl.NumberFormat("pt-BR", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  useGrouping: false,
});

export function formatarDinheiroParaEdicao(valor: number): string {
  return dinheiroParaEdicao.format(valor);
}

export function lerDinheiro(texto: string): number | null {
  const limpo = texto.replace(/R\$/gi, "").replace(/\s/g, "");
  const normalizado = limpo.includes(",") ? limpo.replace(/\./g, "").replace(",", ".") : limpo;
  if (!/^-?\d+(\.\d+)?$/.test(normalizado)) {
    return null;
  }
  return Number(normalizado);
}

export const nomeDoTipoPecaInsumo: Record<TipoPecaInsumo, string> = {
  Peca: "Peça",
  Insumo: "Insumo",
};

export function formatarSaldo(valor: number, unidadeDeMedida: string): string {
  return `${formatarQuantidade(valor)} ${unidadeDeMedida}`;
}

export function lerQuantidade(texto: string): number | null {
  const limpo = texto.trim().replace(/\s/g, "");
  const normalizado = limpo.includes(",") ? limpo.replace(/\./g, "").replace(",", ".") : limpo;
  return /^-?\d+(\.\d+)?$/.test(normalizado) ? Number(normalizado) : null;
}
