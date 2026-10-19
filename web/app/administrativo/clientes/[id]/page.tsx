import { ArrowLeft, Pencil } from "lucide-react";
import Link from "next/link";
import { AtivoOuInativo } from "@/components/ativo-ou-inativo";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { InativarComConfirmacao } from "@/components/inativar-com-confirmacao";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { DadosDoCliente } from "../dados-do-cliente";
import { enderecoDoCliente } from "../endereco-do-cliente";
import { inativarCliente } from "../acoes";
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
        <AtivoOuInativo ativo={cliente.ativo} />
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
                <Link href={`${enderecoDoCliente(cliente.id)}/editar`}>
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
            <InativarComConfirmacao
              rotulo="Inativar Cliente"
              tamanho="default"
              titulo={`Inativar o Cliente ${cliente.nome}?`}
              descricao="O Cliente não poderá ser usado em novas operações, mas continua visível no histórico. Não há como reativá-lo depois."
              acao={inativarCliente.bind(null, cliente.id)}
            />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
