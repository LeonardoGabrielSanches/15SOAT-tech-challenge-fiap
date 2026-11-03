import { ArrowRight } from "lucide-react";
import type { ReactNode } from "react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import type { ConsultaPublicaDaOsDto } from "@/lib/api/dtos";
import {
  formatarDataHora,
  formatarDinheiro,
  formatarQuantidade,
  nomeDaSituacao,
  nomeDoStatus,
} from "@/lib/formatacao";

export function ResultadoDaConsulta({ os, decisao }: { os: ConsultaPublicaDaOsDto; decisao?: ReactNode }) {
  const orcamento = os.orcamentoVigente;

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader>
          <CardDescription>Número da OS</CardDescription>
          <CardTitle className="flex flex-wrap items-center gap-2 text-lg">
            {os.numero}
            <Badge>{nomeDoStatus[os.status]}</Badge>
          </CardTitle>
          <CardDescription>Aberta em {formatarDataHora(os.abertaEm)}</CardDescription>
        </CardHeader>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Orçamento</CardTitle>
          {orcamento && (
            <CardDescription className="flex flex-wrap items-center gap-2">
              Versão {orcamento.versao}
              <Badge variant="outline">{nomeDaSituacao[orcamento.situacao]}</Badge>
            </CardDescription>
          )}
        </CardHeader>
        <CardContent>
          {orcamento ? (
            <div className="flex flex-col">
              <div className="hidden grid-cols-[1fr_6rem_7rem_7rem] gap-3 border-b pb-2 text-xs font-medium text-muted-foreground sm:grid">
                <span>Item</span>
                <span className="text-right">Quantidade</span>
                <span className="text-right">Preço unitário</span>
                <span className="text-right">Subtotal</span>
              </div>
              <ul className="divide-y">
                {orcamento.itens.map((item, indice) => (
                  <li
                    key={`${indice}-${item.descricao}`}
                    className="grid gap-1 py-3 sm:grid-cols-[1fr_6rem_7rem_7rem] sm:items-center sm:gap-3"
                  >
                    <span className="font-medium">{item.descricao}</span>
                    <span className="text-muted-foreground sm:text-right sm:text-foreground">
                      <span className="sm:hidden">
                        {formatarQuantidade(item.quantidade)} × {formatarDinheiro(item.precoUnitario)}
                      </span>
                      <span className="hidden sm:inline">{formatarQuantidade(item.quantidade)}</span>
                    </span>
                    <span className="hidden text-right sm:inline">{formatarDinheiro(item.precoUnitario)}</span>
                    <span className="sm:text-right">
                      <span className="text-muted-foreground sm:hidden">Subtotal: </span>
                      {formatarDinheiro(item.subtotal)}
                    </span>
                  </li>
                ))}
              </ul>
              <div className="flex items-center justify-between border-t pt-3 font-semibold">
                <span>Total</span>
                <span>{formatarDinheiro(orcamento.total)}</span>
              </div>
            </div>
          ) : (
            <p className="text-muted-foreground">Esta OS ainda não tem orçamento.</p>
          )}
        </CardContent>
        {decisao && <CardFooter className="flex-col items-stretch">{decisao}</CardFooter>}
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Histórico de status</CardTitle>
        </CardHeader>
        <CardContent>
          <ol className="flex flex-col gap-3">
            {os.historico.map((entrada, indice) => (
              <li key={`${indice}-${entrada.momento}`} className="flex flex-col gap-1 border-l-2 pl-3">
                <span className="flex flex-wrap items-center gap-1 font-medium">
                  {entrada.origem ? (
                    <>
                      {nomeDoStatus[entrada.origem]}
                      <ArrowRight className="size-4 text-muted-foreground" />
                    </>
                  ) : null}
                  {nomeDoStatus[entrada.destino]}
                </span>
                <span className="text-xs text-muted-foreground">
                  {formatarDataHora(entrada.momento)} · por {entrada.autor}
                </span>
              </li>
            ))}
          </ol>
        </CardContent>
      </Card>
    </div>
  );
}
