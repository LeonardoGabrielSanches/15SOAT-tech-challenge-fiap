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

export const nomeDoTipoDeDocumento: Record<"Cpf" | "Cnpj", string> = {
  Cpf: "CPF",
  Cnpj: "CNPJ",
};

export function formatarDocumento(tipo: "Cpf" | "Cnpj", documento: string): string {
  if (tipo === "Cpf" && documento.length === 11) {
    return documento.replace(/^(.{3})(.{3})(.{3})(.{2})$/, "$1.$2.$3-$4");
  }
  if (tipo === "Cnpj" && documento.length === 14) {
    return documento.replace(/^(.{2})(.{3})(.{3})(.{4})(.{2})$/, "$1.$2.$3/$4-$5");
  }
  return documento;
}

export function formatarTelefone(telefone: string): string {
  if (/^\d{11}$/.test(telefone)) {
    return telefone.replace(/^(\d{2})(\d{5})(\d{4})$/, "($1) $2-$3");
  }
  if (/^\d{10}$/.test(telefone)) {
    return telefone.replace(/^(\d{2})(\d{4})(\d{4})$/, "($1) $2-$3");
  }
  return telefone;
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

const numeroPtBr = /^-?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$/;

function lerNumeroPtBr(texto: string): number | null {
  const limpo = texto.replace(/\s/g, "");
  return numeroPtBr.test(limpo) ? Number(limpo.replace(/\./g, "").replace(",", ".")) : null;
}

export function lerDinheiro(texto: string): number | null {
  return lerNumeroPtBr(texto.replace(/R\$/gi, ""));
}

export const nomeDoTipoPecaInsumo: Record<TipoPecaInsumo, string> = {
  Peca: "Peça",
  Insumo: "Insumo",
};

export function formatarSaldo(valor: number, unidadeDeMedida: string): string {
  return `${formatarQuantidade(valor)} ${unidadeDeMedida}`;
}

export function lerQuantidade(texto: string): number | null {
  return lerNumeroPtBr(texto);
}

export const nomeDoTipoDoItemDaVersao: Record<"Servico" | "PecaInsumo", string> = {
  Servico: "Serviço",
  PecaInsumo: "Peça/insumo",
};
