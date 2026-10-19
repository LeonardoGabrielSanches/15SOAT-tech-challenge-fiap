import { Clock } from "lucide-react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ServicoDto, TempoMedioDeExecucaoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDuracao, formatarQuantidade } from "@/lib/formatacao";
import { FiltroPorServico } from "./filtro-por-servico";

function nomeDaOpcao(servico: ServicoDto): string {
  return servico.ativo ? servico.nome : `${servico.nome} (inativo)`;
}

function textoDaQuantidade(quantidadeDeOs: number): string {
  return quantidadeDeOs === 1 ? "1 OS finalizada" : `${formatarQuantidade(quantidadeDeOs)} OS finalizadas`;
}

export default async function TempoMedioDeExecucao({
  searchParams,
}: PageProps<"/administrativo/tempo-medio-de-execucao">) {
  await exigirTela("tempoMedioDeExecucao");
  const { servicoId } = await searchParams;
  const api = await apiAdministrativa();

  const servicos = await resultadoDaApi<ServicoDto[]>(api.GET("/api/administrativo/servicos"));
  if (!servicos.ok) {
    return <AvisoDeFalha falha={servicos.falha} />;
  }

  const servico = servicos.dados.find((s) => s.id === servicoId) ?? null;
  const tempoMedio = await resultadoDaApi<TempoMedioDeExecucaoDto>(
    api.GET("/api/administrativo/ordens-de-servico/tempo-medio-de-execucao", {
      params: { query: servico ? { servicoId: servico.id } : {} },
    }),
  );

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold">Tempo médio de execução</h1>
        <p className="text-muted-foreground">
          Intervalo entre o início e a finalização da execução, medido para a OS inteira.
        </p>
      </div>

      <FiltroPorServico
        opcoes={servicos.dados.map((s) => ({ id: s.id, nome: nomeDaOpcao(s) }))}
        servicoSelecionado={servico?.id ?? null}
      />

      {!tempoMedio.ok ? (
        <AvisoDeFalha falha={tempoMedio.falha} />
      ) : tempoMedio.dados.tempoMedioEmSegundos === null || tempoMedio.dados.quantidadeDeOs === 0 ? (
        <Alert>
          <Clock />
          <AlertTitle>Nenhuma OS finalizada</AlertTitle>
          <AlertDescription>
            {servico
              ? `Ainda não há OS finalizada com o Serviço ${servico.nome}. O tempo médio aparece quando uma OS com este Serviço tiver a execução finalizada.`
              : "Ainda não há OS finalizada. O tempo médio aparece quando a primeira OS tiver a execução finalizada."}
          </AlertDescription>
        </Alert>
      ) : (
        <Card>
          <CardHeader>
            <CardDescription>{servico ? `OS com o Serviço ${servico.nome}` : "Todas as OS"}</CardDescription>
            <CardTitle className="text-4xl font-semibold tabular-nums">
              {formatarDuracao(tempoMedio.dados.tempoMedioEmSegundos)}
            </CardTitle>
          </CardHeader>
          <CardContent className="text-muted-foreground">
            Calculado sobre {textoDaQuantidade(tempoMedio.dados.quantidadeDeOs)}.
          </CardContent>
        </Card>
      )}
    </div>
  );
}
