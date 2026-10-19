import { Badge } from "@/components/ui/badge";

export function AtivoOuInativo({ ativo }: { ativo: boolean }) {
  return ativo ? <Badge variant="secondary">Ativo</Badge> : <Badge variant="destructive">Inativo</Badge>;
}
