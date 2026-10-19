import Link from "next/link";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { PecaInsumoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { podeGerirPecasInsumos } from "@/lib/capacidades";
import { CadastroDePecaInsumo } from "./cadastro-de-peca-insumo";
import { LinhaDaPecaInsumo } from "./linha-da-peca-insumo";

export default async function PecasInsumos({ searchParams }: PageProps<"/administrativo/pecas-insumos">) {
  const { papel } = await exigirTela("pecasInsumos");
  const { inativos } = await searchParams;
  const incluirInativos = inativos === "1";
  const podeEscrever = podeGerirPecasInsumos(papel);

  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<PecaInsumoDto[]>(
    api.GET("/api/pecas-insumos", { params: { query: { incluirInativos } } }),
  );

  return (
    <div className="flex max-w-6xl flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold">Peças/insumos</h1>
        <p className="text-muted-foreground">
          {podeEscrever
            ? "Cadastre, edite e inative Peças/insumos e registre Entradas de mercadoria."
            : "Consulta de Peças/insumos e saldos em estoque."}
        </p>
      </div>

      {podeEscrever && <CadastroDePecaInsumo />}

      <section className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="text-lg font-medium">{incluirInativos ? "Todas as Peças/insumos" : "Peças/insumos ativas"}</h2>
          <Button asChild variant="outline" size="sm">
            <Link href={incluirInativos ? "/administrativo/pecas-insumos" : "/administrativo/pecas-insumos?inativos=1"}>
              {incluirInativos ? "Ocultar inativas" : "Incluir inativas"}
            </Link>
          </Button>
        </div>

        {!resultado.ok ? (
          <AvisoDeFalha falha={resultado.falha} />
        ) : resultado.dados.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhuma Peça/insumo encontrada.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Nome</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead>Unidade</TableHead>
                <TableHead className="text-right">Preço</TableHead>
                <TableHead className="text-right">Saldo</TableHead>
                {incluirInativos && <TableHead>Situação</TableHead>}
                {podeEscrever && <TableHead className="text-right">Ações</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {resultado.dados.map((item) => (
                <LinhaDaPecaInsumo
                  key={item.id}
                  item={item}
                  mostrarSituacao={incluirInativos}
                  podeEscrever={podeEscrever}
                />
              ))}
            </TableBody>
          </Table>
        )}
      </section>
    </div>
  );
}
