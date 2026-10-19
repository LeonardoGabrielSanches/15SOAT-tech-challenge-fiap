export const papeis = ["atendente", "mecanico", "gerente"] as const;

export type Papel = (typeof papeis)[number];

export const nomeDoPapel: Record<Papel, string> = {
  atendente: "Atendente",
  mecanico: "Mecânico",
  gerente: "Gerente",
};

export type Tela = {
  id: TelaId;
  titulo: string;
  href: string;
};

const definicoesDeTelas = {
  balcao: { titulo: "Balcão", href: "/administrativo/balcao", papeis: ["atendente"] },
  clientes: { titulo: "Clientes", href: "/administrativo/clientes", papeis: ["atendente"] },
  ordensDeServico: { titulo: "OS", href: "/administrativo/ordens-de-servico", papeis: ["atendente"] },
  filaDeExecucao: { titulo: "Fila de execução", href: "/administrativo/fila-de-execucao", papeis: ["mecanico"] },
  abrirOs: { titulo: "Abrir OS por link/identificador", href: "/administrativo/abrir-os", papeis: ["mecanico"] },
  servicos: { titulo: "Serviços", href: "/administrativo/servicos", papeis: [] },
  pecasInsumos: { titulo: "Peças/insumos", href: "/administrativo/pecas-insumos", papeis: ["mecanico"] },
  tempoMedioDeExecucao: {
    titulo: "Tempo médio de execução",
    href: "/administrativo/tempo-medio-de-execucao",
    papeis: [],
  },
} as const satisfies Record<string, { titulo: string; href: string; papeis: readonly Papel[] }>;

export type TelaId = keyof typeof definicoesDeTelas;

const telaInicialDoPapel: Record<Papel, TelaId> = {
  atendente: "balcao",
  mecanico: "filaDeExecucao",
  gerente: "servicos",
};

const ordemDePrecedencia: readonly Papel[] = ["gerente", "atendente", "mecanico"];

export function papelPrincipal(papeisDoUsuario: readonly string[]): Papel | null {
  return ordemDePrecedencia.find((papel) => papeisDoUsuario.includes(papel)) ?? null;
}

export function permite(papel: Papel, papeisPermitidos: readonly Papel[]): boolean {
  return papel === "gerente" || papeisPermitidos.includes(papel);
}

export function tela(id: TelaId): Tela {
  const { titulo, href } = definicoesDeTelas[id];
  return { id, titulo, href };
}

export function podeAcessarTela(papel: Papel, id: TelaId): boolean {
  return permite(papel, definicoesDeTelas[id].papeis);
}

export function telasDoMenu(papel: Papel): Tela[] {
  return (Object.keys(definicoesDeTelas) as TelaId[]).filter((id) => podeAcessarTela(papel, id)).map(tela);
}

export function telaInicial(papel: Papel): Tela {
  return tela(telaInicialDoPapel[papel]);
}

export function podeGerirPecasInsumos(papel: Papel): boolean {
  return permite(papel, []);
}
