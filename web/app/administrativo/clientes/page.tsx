import { Search } from "lucide-react";
import Link from "next/link";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDocumento, formatarTelefone } from "@/lib/formatacao";
import { cn } from "@/lib/utils";
import { SituacaoDoCliente } from "./situacao-do-cliente";

function TabelaDeClientes({ clientes }: { clientes: ClienteDto[] }) {
  return (
    <div className="overflow-x-auto rounded-xl ring-1 ring-foreground/10">
      <table className="w-full text-left text-sm">
        <thead className="bg-muted/50 text-xs text-muted-foreground">
          <tr>
            <th className="px-3 py-2 font-medium">Nome</th>
            <th className="px-3 py-2 font-medium">Documento</th>
            <th className="px-3 py-2 font-medium">E-mail</th>
            <th className="px-3 py-2 font-medium">Telefone</th>
            <th className="px-3 py-2 font-medium">Situação</th>
          </tr>
        </thead>
        <tbody>
          {clientes.map((cliente) => (
            <tr key={cliente.id} className={cn("border-t", !cliente.ativo && "text-muted-foreground")}>
              <td className="px-3 py-2">
                <Link href={`/administrativo/clientes/${cliente.id}`} className="font-medium underline-offset-4 hover:underline">
                  {cliente.nome}
                </Link>
              </td>
              <td className="px-3 py-2 whitespace-nowrap">{formatarDocumento(cliente.tipoDocumento, cliente.documento)}</td>
              <td className="px-3 py-2">{cliente.email}</td>
              <td className="px-3 py-2 whitespace-nowrap">{cliente.telefone ? formatarTelefone(cliente.telefone) : "—"}</td>
              <td className="px-3 py-2">
                <SituacaoDoCliente ativo={cliente.ativo} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
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
