import { ArrowRight, Inbox } from "lucide-react";
import Link from "next/link";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { OrdemDeServicoDetalheDto, OrdemDeServicoResumoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDataHora } from "@/lib/formatacao";
import { enderecoDaOs } from "../ordens-de-servico/endereco-da-os";

type Api = Awaited<ReturnType<typeof apiAdministrativa>>;

async function detalhesPorId(api: Api, ids: string[]): Promise<Map<string, OrdemDeServicoDetalheDto>> {
  const resultados = await Promise.all(
    ids.map((id) =>
      resultadoDaApi<OrdemDeServicoDetalheDto>(
        api.GET("/api/administrativo/ordens-de-servico/{id}", { params: { path: { id } } }),
      ),
    ),
  );
  return new Map(resultados.flatMap((resultado) => (resultado.ok ? [[resultado.dados.id, resultado.dados]] : [])));
}

function FilaVazia() {
  return (
    <div className="flex flex-col items-center gap-2 rounded-xl px-6 py-12 text-center ring-1 ring-foreground/10">
      <Inbox className="size-8 text-muted-foreground" />
      <p className="font-medium">Nenhuma OS Aprovada aguardando execução</p>
      <p className="max-w-md text-sm text-muted-foreground">
        Assim que o Cliente aprovar um orçamento, a OS aparece aqui, na ordem de abertura.
      </p>
    </div>
  );
}

function TabelaDaFila({
  ordens,
  detalhes,
}: {
  ordens: OrdemDeServicoResumoDto[];
  detalhes: Map<string, OrdemDeServicoDetalheDto>;
}) {
  return (
    <div className="rounded-xl ring-1 ring-foreground/10">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Número da OS</TableHead>
            <TableHead>Veículo</TableHead>
            <TableHead>Cliente</TableHead>
            <TableHead>Abertura</TableHead>
            <TableHead>
              <span className="sr-only">Ações</span>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {ordens.map((os) => {
            const detalhe = detalhes.get(os.id);
            return (
              <TableRow key={os.id}>
                <TableCell>
                  <Link href={enderecoDaOs(os.id)} className="font-mono font-medium underline-offset-4 hover:underline">
                    {os.numero}
                  </Link>
                </TableCell>
                <TableCell>
                  {detalhe ? (
                    <span>
                      <span className="font-mono font-medium">{detalhe.veiculo.placa}</span>{" "}
                      <span className="text-muted-foreground">
                        {detalhe.veiculo.marca} {detalhe.veiculo.modelo}
                      </span>
                    </span>
                  ) : (
                    "—"
                  )}
                </TableCell>
                <TableCell>{detalhe?.cliente.nome ?? "—"}</TableCell>
                <TableCell className="tabular-nums">{formatarDataHora(os.abertaEm)}</TableCell>
                <TableCell className="text-right">
                  <Button asChild size="sm" variant="outline">
                    <Link href={enderecoDaOs(os.id)}>
                      Abrir OS
                      <ArrowRight />
                    </Link>
                  </Button>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}

export default async function FilaDeExecucao() {
  await exigirTela("filaDeExecucao");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<OrdemDeServicoResumoDto[]>(
    api.GET("/api/administrativo/ordens-de-servico/fila-de-execucao"),
  );
  const ordens = resultado.ok ? resultado.dados : [];
  const detalhes = await detalhesPorId(
    api,
    ordens.map((os) => os.id),
  );

  return (
    <div className="flex max-w-6xl flex-col gap-4">
      <h1 className="text-2xl font-semibold">Fila de execução</h1>
      <p className="text-sm text-muted-foreground">
        OS Aprovadas pelo Cliente, da mais antiga para a mais recente. Abra uma OS para iniciar a execução.
      </p>
      {!resultado.ok ? (
        <AvisoDeFalha falha={resultado.falha} />
      ) : ordens.length === 0 ? (
        <FilaVazia />
      ) : (
        <TabelaDaFila ordens={ordens} detalhes={detalhes} />
      )}
    </div>
  );
}
