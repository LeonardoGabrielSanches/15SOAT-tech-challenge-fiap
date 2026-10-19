import { Badge } from "@/components/ui/badge";
import type { SituacaoDaVersao, StatusOs } from "@/lib/api/dtos";
import { nomeDaSituacao, nomeDoStatus } from "@/lib/formatacao";

type Variante = "default" | "secondary" | "destructive" | "outline";

const varianteDoStatus: Record<StatusOs, Variante> = {
  Recebida: "outline",
  EmDiagnostico: "default",
  AguardandoAprovacao: "outline",
  Aprovada: "default",
  EmExecucao: "default",
  Finalizada: "secondary",
  Entregue: "secondary",
  Cancelada: "destructive",
};

const varianteDaSituacao: Record<SituacaoDaVersao, Variante> = {
  Pendente: "outline",
  Aprovada: "default",
  Recusada: "destructive",
  Superada: "secondary",
};

export function StatusDaOs({ status }: { status: StatusOs }) {
  return <Badge variant={varianteDoStatus[status]}>{nomeDoStatus[status]}</Badge>;
}

export function SituacaoDaVersaoDoOrcamento({ situacao }: { situacao: SituacaoDaVersao }) {
  return <Badge variant={varianteDaSituacao[situacao]}>{nomeDaSituacao[situacao]}</Badge>;
}
