import { ArrowLeft, Pencil } from "lucide-react";
import Link from "next/link";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { DadosDoCliente } from "../dados-do-cliente";
import { SituacaoDoCliente } from "../situacao-do-cliente";
import { InativarCliente } from "./inativar-cliente";
import { VeiculosDoCliente } from "./veiculos-do-cliente";

function VoltarParaClientes() {
  return (
    <Button asChild variant="ghost" size="sm" className="self-start">
      <Link href="/administrativo/clientes">
        <ArrowLeft />
        Clientes
      </Link>
    </Button>
  );
}

export default async function PaginaDoCliente({ params }: PageProps<"/administrativo/clientes/[id]">) {
  await exigirTela("clientes");
  const { id } = await params;
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ClienteDto>(
    api.GET("/api/administrativo/clientes/{id}", { params: { path: { id } } }),
  );

  if (!resultado.ok) {
    return (
      <div className="flex max-w-4xl flex-col gap-4">
        <VoltarParaClientes />
        <AvisoDeFalha falha={resultado.falha} />
      </div>
    );
  }

  const cliente = resultado.dados;

  return (
    <div className="flex max-w-4xl flex-col gap-6">
      <VoltarParaClientes />
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-semibold break-words">{cliente.nome}</h1>
        <SituacaoDoCliente ativo={cliente.ativo} />
      </div>

      {!cliente.ativo && (
        <Alert variant="destructive">
          <AlertTitle>Cliente Inativo</AlertTitle>
          <AlertDescription>
            Este Cliente não pode ser usado em novas operações, como abrir uma OS ou cadastrar um Veículo. Os dados
            continuam visíveis no histórico.
          </AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Dados do Cliente</CardTitle>
          <CardDescription>Contato usado pela oficina.</CardDescription>
          {cliente.ativo && (
            <CardAction>
              <Button asChild variant="outline" size="sm">
                <Link href={`/administrativo/clientes/${cliente.id}/editar`}>
                  <Pencil />
                  Editar
                </Link>
              </Button>
            </CardAction>
          )}
        </CardHeader>
        <CardContent>
          <DadosDoCliente cliente={cliente} />
        </CardContent>
      </Card>

      <VeiculosDoCliente cliente={cliente} />

      {cliente.ativo && (
        <Card>
          <CardHeader>
            <CardTitle>Inativar Cliente</CardTitle>
            <CardDescription>Retira o Cliente de novas operações sem perder o histórico.</CardDescription>
          </CardHeader>
          <CardContent>
            <InativarCliente id={cliente.id} nome={cliente.nome} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
