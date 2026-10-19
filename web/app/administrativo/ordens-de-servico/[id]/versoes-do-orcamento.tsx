import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { VersaoDoOrcamentoDto } from "@/lib/api/dtos";
import { formatarDataHora, formatarDinheiro, formatarQuantidade, nomeDoTipoDoItemDaVersao } from "@/lib/formatacao";
import { SituacaoDaVersaoDoOrcamento } from "../status-da-os";

function VersaoDoOrcamento({ versao }: { versao: VersaoDoOrcamentoDto }) {
  return (
    <section className="flex flex-col gap-3 rounded-lg border p-4" aria-labelledby={`versao-${versao.numero}`}>
      <div className="flex flex-wrap items-center gap-2">
        <h3 id={`versao-${versao.numero}`} className="font-medium">
          Versão {versao.numero}
        </h3>
        <SituacaoDaVersaoDoOrcamento situacao={versao.situacao} />
        <span className="text-sm text-muted-foreground">gerada em {formatarDataHora(versao.geradaEm)}</span>
      </div>
      {versao.motivoDaRecusa && (
        <p className="text-sm">
          <span className="text-muted-foreground">Motivo da recusa: </span>
          {versao.motivoDaRecusa}
        </p>
      )}
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Item</TableHead>
            <TableHead>Tipo</TableHead>
            <TableHead className="text-right">Quantidade</TableHead>
            <TableHead className="text-right">Preço unitário</TableHead>
            <TableHead className="text-right">Subtotal</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {versao.itens.map((item) => (
            <TableRow key={`${item.tipo}-${item.referenciaId}`}>
              <TableCell className="whitespace-normal">{item.descricao}</TableCell>
              <TableCell className="text-muted-foreground">{nomeDoTipoDoItemDaVersao[item.tipo]}</TableCell>
              <TableCell className="text-right tabular-nums">{formatarQuantidade(item.quantidade)}</TableCell>
              <TableCell className="text-right tabular-nums">{formatarDinheiro(item.precoUnitario)}</TableCell>
              <TableCell className="text-right tabular-nums">{formatarDinheiro(item.subtotal)}</TableCell>
            </TableRow>
          ))}
          <TableRow className="font-medium">
            <TableCell colSpan={4}>Total</TableCell>
            <TableCell className="text-right tabular-nums">{formatarDinheiro(versao.total)}</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </section>
  );
}

export function VersoesDoOrcamento({ versoes }: { versoes: VersaoDoOrcamentoDto[] }) {
  const ordenadas = [...versoes].sort((a, b) => b.numero - a.numero);

  return (
    <Card id="versoes-do-orcamento">
      <CardHeader>
        <CardTitle>Versões do orçamento</CardTitle>
        <CardDescription>Da mais recente para a mais antiga, com os preços congelados em cada uma.</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {ordenadas.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Nenhuma Versão do orçamento ainda. Ela é gerada quando o diagnóstico é concluído.
          </p>
        ) : (
          ordenadas.map((versao) => <VersaoDoOrcamento key={versao.numero} versao={versao} />)
        )}
      </CardContent>
    </Card>
  );
}
