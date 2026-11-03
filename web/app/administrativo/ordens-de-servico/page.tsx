import { ChevronLeft, ChevronRight, Search } from "lucide-react";
import Link from "next/link";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto, OrdemDeServicoResumoDto, PaginaDeOrdensDeServicoDto, StatusOs, VeiculoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDataHora, nomeDoStatus } from "@/lib/formatacao";
import { enderecoDaOs } from "./endereco-da-os";
import { enderecoDaListagem, FiltroPorStatus, lerStatusDaOs } from "./filtro-por-status";
import { StatusDaOs } from "./status-da-os";

type Api = Awaited<ReturnType<typeof apiAdministrativa>>;

async function clientesPorId(api: Api, ids: string[]): Promise<Map<string, ClienteDto>> {
  const resultados = await Promise.all(
    [...new Set(ids)].map((id) =>
      resultadoDaApi<ClienteDto>(api.GET("/api/administrativo/clientes/{id}", { params: { path: { id } } })),
    ),
  );
  return new Map(resultados.flatMap((resultado) => (resultado.ok ? [[resultado.dados.id, resultado.dados]] : [])));
}

async function veiculosPorId(api: Api, ids: string[]): Promise<Map<string, VeiculoDto>> {
  const resultados = await Promise.all(
    [...new Set(ids)].map((id) =>
      resultadoDaApi<VeiculoDto>(api.GET("/api/administrativo/veiculos/{id}", { params: { path: { id } } })),
    ),
  );
  return new Map(resultados.flatMap((resultado) => (resultado.ok ? [[resultado.dados.id, resultado.dados]] : [])));
}

function TabelaDeOs({
  ordens,
  clientes,
  veiculos,
}: {
  ordens: OrdemDeServicoResumoDto[];
  clientes: Map<string, ClienteDto>;
  veiculos: Map<string, VeiculoDto>;
}) {
  return (
    <div className="rounded-xl ring-1 ring-foreground/10">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Número da OS</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Cliente</TableHead>
            <TableHead>Veículo</TableHead>
            <TableHead>Abertura</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {ordens.map((os) => {
            const cliente = clientes.get(os.clienteId);
            const veiculo = veiculos.get(os.veiculoId);
            return (
              <TableRow key={os.id}>
                <TableCell>
                  <Link
                    href={enderecoDaOs(os.id)}
                    className="font-mono font-medium underline-offset-4 hover:underline"
                  >
                    {os.numero}
                  </Link>
                </TableCell>
                <TableCell>
                  <StatusDaOs status={os.status} />
                </TableCell>
                <TableCell>{cliente?.nome ?? "—"}</TableCell>
                <TableCell>
                  {veiculo ? (
                    <span>
                      <span className="font-mono font-medium">{veiculo.placa}</span>{" "}
                      <span className="text-muted-foreground">
                        {veiculo.marca} {veiculo.modelo}
                      </span>
                    </span>
                  ) : (
                    "—"
                  )}
                </TableCell>
                <TableCell className="tabular-nums">{formatarDataHora(os.abertaEm)}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}

function lerPagina(valor: string | string[] | undefined): number {
  const pagina = typeof valor === "string" ? Number(valor) : NaN;
  return Number.isInteger(pagina) && pagina >= 1 ? pagina : 1;
}

function NavegacaoEntrePaginas({ resultado, status }: { resultado: PaginaDeOrdensDeServicoDto; status: StatusOs | null }) {
  const totalDePaginas = Math.max(1, Math.ceil(resultado.total / resultado.tamanho));
  const anterior = resultado.pagina > 1 ? enderecoDaListagem(status, resultado.pagina - 1) : null;
  const proxima = resultado.pagina < totalDePaginas ? enderecoDaListagem(status, resultado.pagina + 1) : null;

  return (
    <nav aria-label="Páginas" className="flex flex-wrap items-center gap-3">
      <Button asChild={anterior !== null} size="sm" variant="outline" disabled={anterior === null}>
        {anterior ? (
          <Link href={anterior}>
            <ChevronLeft />
            Anterior
          </Link>
        ) : (
          <span>
            <ChevronLeft />
            Anterior
          </span>
        )}
      </Button>
      <span className="text-sm tabular-nums text-muted-foreground">
        Página {Math.min(resultado.pagina, totalDePaginas)} de {totalDePaginas} ({resultado.total}{" "}
        {resultado.total === 1 ? "OS" : "OS no total"})
      </span>
      <Button asChild={proxima !== null} size="sm" variant="outline" disabled={proxima === null}>
        {proxima ? (
          <Link href={proxima}>
            Próxima
            <ChevronRight />
          </Link>
        ) : (
          <span>
            Próxima
            <ChevronRight />
          </span>
        )}
      </Button>
    </nav>
  );
}

export default async function OrdensDeServico({ searchParams }: PageProps<"/administrativo/ordens-de-servico">) {
  await exigirTela("ordensDeServico");
  const parametros = await searchParams;
  const status = lerStatusDaOs(parametros.status);
  const pagina = lerPagina(parametros.pagina);
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<PaginaDeOrdensDeServicoDto>(
    api.GET("/api/administrativo/ordens-de-servico", {
      params: { query: status ? { status, pagina } : { pagina } },
    }),
  );
  const ordens = resultado.ok ? resultado.dados.itens : [];
  const [clientes, veiculos] = await Promise.all([
    clientesPorId(
      api,
      ordens.map((os) => os.clienteId),
    ),
    veiculosPorId(
      api,
      ordens.map((os) => os.veiculoId),
    ),
  ]);

  return (
    <div className="flex max-w-6xl flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">OS</h1>
        <Button asChild size="sm" variant="outline">
          <Link href="/administrativo/balcao">
            <Search />
            Abrir OS pelo Balcão
          </Link>
        </Button>
      </div>
      <FiltroPorStatus statusSelecionado={status} />
      <p className="text-sm text-muted-foreground">
        {status
          ? `Mostrando as OS com Status ${nomeDoStatus[status]}, na ordem de prioridade da oficina.`
          : "Mostrando as OS que ainda exigem trabalho (sem Finalizadas, Entregues e Canceladas), na ordem de prioridade da oficina. Use o filtro Finalizada para registrar a entrega."}
      </p>
      {!resultado.ok ? (
        <AvisoDeFalha falha={resultado.falha} />
      ) : ordens.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          {status ? `Nenhuma OS com Status ${nomeDoStatus[status]}.` : "Nenhuma OS pendente."}
        </p>
      ) : (
        <>
          <TabelaDeOs ordens={ordens} clientes={clientes} veiculos={veiculos} />
          <NavegacaoEntrePaginas resultado={resultado.dados} status={status} />
        </>
      )}
    </div>
  );
}
