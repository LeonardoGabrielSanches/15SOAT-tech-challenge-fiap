import type { Falha } from "@/lib/api/resultado";

export type ValoresDoCliente = {
  nome: string;
  documento: string;
  email: string;
  telefone: string;
};

export type EstadoDoFormularioDeCliente = {
  falha: Falha | null;
  valores: ValoresDoCliente;
  tentativa: number;
};

export type CampoDoCliente = keyof ValoresDoCliente;

export function estadoInicial(valores: Partial<ValoresDoCliente>): EstadoDoFormularioDeCliente {
  return {
    falha: null,
    valores: { nome: "", documento: "", email: "", telefone: "", ...valores },
    tentativa: 0,
  };
}

export function campoDaFalha(falha: Falha | null): CampoDoCliente | null {
  if (!falha) {
    return null;
  }
  const mensagem = falha.mensagem.toLowerCase();
  if (mensagem.includes("documento") || mensagem.includes("cpf") || mensagem.includes("cnpj")) {
    return "documento";
  }
  if (mensagem.includes("e-mail")) {
    return "email";
  }
  if (mensagem.includes("telefone")) {
    return "telefone";
  }
  if (mensagem.includes("nome")) {
    return "nome";
  }
  return null;
}
