import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { HistoricoDeStatusDto } from "@/lib/api/dtos";
import { formatarDataHora, nomeDoStatus } from "@/lib/formatacao";

export function HistoricoDeStatus({ historico }: { historico: HistoricoDeStatusDto[] }) {
  const ordenado = [...historico].sort((a, b) => Date.parse(a.momento) - Date.parse(b.momento));

  return (
    <Card id="historico-de-status">
      <CardHeader>
        <CardTitle>Histórico de status</CardTitle>
        <CardDescription>Cada mudança de Status, da abertura até agora.</CardDescription>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>De</TableHead>
              <TableHead>Para</TableHead>
              <TableHead>Momento</TableHead>
              <TableHead>Autor</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {ordenado.map((entrada, indice) => (
              <TableRow key={`${entrada.momento}-${indice}`}>
                <TableCell className="text-muted-foreground">
                  {entrada.origem ? nomeDoStatus[entrada.origem] : "Abertura"}
                </TableCell>
                <TableCell className="font-medium">{nomeDoStatus[entrada.destino]}</TableCell>
                <TableCell className="tabular-nums">{formatarDataHora(entrada.momento)}</TableCell>
                <TableCell>{entrada.autor}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}
