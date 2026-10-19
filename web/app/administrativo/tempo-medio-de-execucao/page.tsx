import { Clock } from "lucide-react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { apiAdministrativa } from "@/lib/api/cliente";
import type {
  ServicoDaCombinacaoDto,
  TempoMedioDaCombinacaoDto,
  TempoMedioDeExecucaoDto,
} from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDuracao, formatarQuantidade } from "@/lib/formatacao";

function nomeDoServico(servico: ServicoDaCombinacaoDto): string {
  return servico.ativo ? servico.nome : `${servico.nome} (inativo)`;
}

function textoDaQuantidade(quantidadeDeOs: number): string {
  return quantidadeDeOs === 1 ? "1 OS finalizada" : `${formatarQuantidade(quantidadeDeOs)} OS finalizadas`;
}

function chaveDaCombinacao(combinacao: TempoMedioDaCombinacaoDto): string {
  return combinacao.servicos.map((s) => s.id).join(",");
}

export default async function TempoMedioDeExecucao() {
  await exigirTela("tempoMedioDeExecucao");
  const api = await apiAdministrativa();
  const tempoMedio = await resultadoDaApi<TempoMedioDeExecucaoDto>(
    api.GET("/api/administrativo/ordens-de-servico/tempo-medio-de-execucao"),
  );

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold">Tempo médio de execução</h1>
        <p className="text-muted-foreground">
          Tempo corrido entre o início e a finalização da execução, medido para a OS inteira: noites, fins de semana e
          esperas entram no tempo. Para comparar OS equivalentes, a média é separada por Combinação de serviços, o
          conjunto exato de Serviços da OS.
        </p>
      </div>

      {!tempoMedio.ok ? (
        <AvisoDeFalha falha={tempoMedio.falha} />
      ) : tempoMedio.dados.tempoMedioEmSegundos === null || tempoMedio.dados.quantidadeDeOs === 0 ? (
        <Alert>
          <Clock />
          <AlertTitle>Nenhuma OS finalizada</AlertTitle>
          <AlertDescription>
            Ainda não há OS finalizada. O tempo médio aparece quando a primeira OS tiver a execução finalizada.
          </AlertDescription>
        </Alert>
      ) : (
        <>
          <Card>
            <CardHeader>
              <CardDescription>Todas as OS</CardDescription>
              <CardTitle className="text-4xl font-semibold tabular-nums">
                {formatarDuracao(tempoMedio.dados.tempoMedioEmSegundos)}
              </CardTitle>
            </CardHeader>
            <CardContent className="text-muted-foreground">
              Calculado sobre {textoDaQuantidade(tempoMedio.dados.quantidadeDeOs)}.
            </CardContent>
          </Card>

          <div className="flex flex-col gap-2">
            <div>
              <h2 className="text-lg font-semibold">Por Combinação de serviços</h2>
              <p className="text-sm text-muted-foreground">
                Das combinações mais frequentes para as menos frequentes. Médias com poucas OS são menos confiáveis.
              </p>
            </div>
            <TabelaDeCombinacoes combinacoes={tempoMedio.dados.combinacoes} />
          </div>
        </>
      )}
    </div>
  );
}

function TabelaDeCombinacoes({ combinacoes }: { combinacoes: TempoMedioDaCombinacaoDto[] }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Combinação de serviços</TableHead>
          <TableHead className="text-right">OS</TableHead>
          <TableHead className="text-right">Tempo médio</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {combinacoes.map((combinacao) => (
          <TableRow key={chaveDaCombinacao(combinacao)}>
            <TableCell className="whitespace-normal font-medium">
              {combinacao.servicos.map(nomeDoServico).join(" + ")}
            </TableCell>
            <TableCell className="text-right tabular-nums">{formatarQuantidade(combinacao.quantidadeDeOs)}</TableCell>
            <TableCell className="text-right tabular-nums">{formatarDuracao(combinacao.tempoMedioEmSegundos)}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
