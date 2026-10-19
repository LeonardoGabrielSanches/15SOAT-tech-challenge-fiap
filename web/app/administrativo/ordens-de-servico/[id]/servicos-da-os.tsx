import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { permiteAcaoNaOs } from "@/lib/capacidades";
import { formatarDinheiro } from "@/lib/formatacao";
import { IncluirServicoNaOs, RemoverServicoDaOs } from "./acoes/acoes-da-os";
import type { PropsDaAcaoDaOs } from "./acoes/tipos";

export function ServicosDaOs({ os, papel }: PropsDaAcaoDaOs) {
  const podeRemover = permiteAcaoNaOs(os.status, papel, "removerServico");
  const total = os.servicos.reduce((soma, servico) => soma + servico.preco, 0);

  return (
    <Card id="servicos">
      <CardHeader>
        <CardTitle>Serviços da OS</CardTitle>
        <CardDescription>
          {os.versoesDoOrcamento.length > 0
            ? "Preços congelados na Versão do orçamento mais recente. Um Serviço incluído depois dela aparece com o preço atual do catálogo."
            : "Preços atuais do catálogo."}
        </CardDescription>
        <CardAction>
          <IncluirServicoNaOs os={os} papel={papel} />
        </CardAction>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Serviço</TableHead>
              <TableHead className="text-right">Preço</TableHead>
              {podeRemover && <TableHead className="text-right">Ações</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {os.servicos.map((servico) => (
              <TableRow key={servico.servicoId}>
                <TableCell className="font-medium whitespace-normal">{servico.nome}</TableCell>
                <TableCell className="text-right tabular-nums">{formatarDinheiro(servico.preco)}</TableCell>
                {podeRemover && (
                  <TableCell>
                    <div className="flex justify-end">
                      <RemoverServicoDaOs os={os} papel={papel} servico={servico} />
                    </div>
                  </TableCell>
                )}
              </TableRow>
            ))}
            <TableRow className="font-medium">
              <TableCell>Total dos Serviços</TableCell>
              <TableCell className="text-right tabular-nums">{formatarDinheiro(total)}</TableCell>
              {podeRemover && <TableCell />}
            </TableRow>
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}
