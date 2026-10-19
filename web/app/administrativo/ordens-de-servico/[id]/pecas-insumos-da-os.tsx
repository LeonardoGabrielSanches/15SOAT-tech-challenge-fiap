import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { permiteAcaoNaOs } from "@/lib/capacidades";
import { formatarDinheiro, formatarQuantidade } from "@/lib/formatacao";
import { IncluirPecaInsumoNaOs, RemoverPecaInsumoDaOs } from "./acoes/acoes-da-os";
import type { PropsDaAcaoDaOs } from "./acoes/tipos";

export function PecasInsumosDaOs({ os, papel }: PropsDaAcaoDaOs) {
  const podeRemover = permiteAcaoNaOs(os.status, papel, "removerPecaInsumo");

  return (
    <Card id="pecas-insumos">
      <CardHeader>
        <CardTitle>Peças/insumos</CardTitle>
        <CardDescription>Baixados do estoque. Preço unitário atual do catálogo.</CardDescription>
        <CardAction>
          <IncluirPecaInsumoNaOs os={os} papel={papel} />
        </CardAction>
      </CardHeader>
      <CardContent>
        {os.pecasInsumos.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhuma Peça/insumo incluída nesta OS.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Peça/insumo</TableHead>
                <TableHead className="text-right">Quantidade</TableHead>
                <TableHead className="text-right">Preço unitário</TableHead>
                <TableHead className="text-right">Subtotal</TableHead>
                {podeRemover && <TableHead className="text-right">Ações</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {os.pecasInsumos.map((pecaInsumo) => (
                <TableRow key={pecaInsumo.pecaInsumoId}>
                  <TableCell className="font-medium whitespace-normal">{pecaInsumo.nome}</TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatarQuantidade(pecaInsumo.quantidade)} {pecaInsumo.unidadeDeMedida}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatarDinheiro(pecaInsumo.preco)}/{pecaInsumo.unidadeDeMedida}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatarDinheiro(pecaInsumo.quantidade * pecaInsumo.preco)}
                  </TableCell>
                  {podeRemover && (
                    <TableCell>
                      <div className="flex justify-end">
                        <RemoverPecaInsumoDaOs os={os} papel={papel} pecaInsumo={pecaInsumo} />
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>
    </Card>
  );
}
