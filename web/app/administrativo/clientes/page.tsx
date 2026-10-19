import { Search } from "lucide-react";
import Link from "next/link";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDocumento, formatarTelefone } from "@/lib/formatacao";
import { cn } from "@/lib/utils";
import { enderecoDoCliente } from "./endereco-do-cliente";
import { SituacaoDoCliente } from "./situacao-do-cliente";

function TabelaDeClientes({ clientes }: { clientes: ClienteDto[] }) {
  return (
    <div className="rounded-xl ring-1 ring-foreground/10">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Nome</TableHead>
            <TableHead>Documento</TableHead>
            <TableHead>E-mail</TableHead>
            <TableHead>Telefone</TableHead>
            <TableHead>Situação</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {clientes.map((cliente) => (
            <TableRow key={cliente.id} className={cn(!cliente.ativo && "text-muted-foreground")}>
              <TableCell>
                <Link
                  href={enderecoDoCliente(cliente.id)}
                  className="font-medium underline-offset-4 hover:underline"
                >
                  {cliente.nome}
                </Link>
              </TableCell>
              <TableCell>{formatarDocumento(cliente.tipoDocumento, cliente.documento)}</TableCell>
              <TableCell>{cliente.email}</TableCell>
              <TableCell>{cliente.telefone ? formatarTelefone(cliente.telefone) : "—"}</TableCell>
              <TableCell>
                <SituacaoDoCliente ativo={cliente.ativo} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

export default async function Clientes({ searchParams }: PageProps<"/administrativo/clientes">) {
  await exigirTela("clientes");
  const { inativos } = await searchParams;
  const incluirInativos = inativos === "1";
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ClienteDto[]>(
    api.GET("/api/administrativo/clientes", { params: { query: incluirInativos ? {} : { ativo: true } } }),
  );

  return (
    <div className="flex max-w-6xl flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">Clientes</h1>
        <div className="flex flex-wrap gap-2">
          <Button asChild variant="outline" size="sm">
            <Link href={incluirInativos ? "/administrativo/clientes" : "/administrativo/clientes?inativos=1"}>
              {incluirInativos ? "Ocultar Inativos" : "Incluir Inativos"}
            </Link>
          </Button>
          <Button asChild size="sm">
            <Link href="/administrativo/balcao">
              <Search />
              Buscar ou cadastrar no Balcão
            </Link>
          </Button>
        </div>
      </div>
      <p className="text-sm text-muted-foreground">
        {incluirInativos ? "Mostrando Clientes ativos e Inativos." : "Mostrando apenas Clientes ativos."}
      </p>
      {!resultado.ok ? (
        <AvisoDeFalha falha={resultado.falha} />
      ) : resultado.dados.length === 0 ? (
        <p className="text-sm text-muted-foreground">Nenhum Cliente cadastrado.</p>
      ) : (
        <TabelaDeClientes clientes={resultado.dados} />
      )}
    </div>
  );
}
