import { ArrowRight, Search, UserPlus } from "lucide-react";
import Form from "next/form";
import Link from "next/link";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto } from "@/lib/api/dtos";
import { resultadoDaApi, type Resultado } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { cadastrarCliente } from "../clientes/acoes";
import { DadosDoCliente } from "../clientes/dados-do-cliente";
import { enderecoDoCliente } from "../clientes/endereco-do-cliente";
import { FormularioDeCliente } from "../clientes/formulario-de-cliente";
import { SituacaoDoCliente } from "../clientes/situacao-do-cliente";

function BuscaPorDocumento({ documento, falha }: { documento: string; falha: boolean }) {
  return (
    <Form action="/administrativo/balcao" className="flex flex-col gap-2">
      <Label htmlFor="busca-documento" className="text-base">
        CPF ou CNPJ do Cliente
      </Label>
      <div className="flex flex-col gap-2 sm:flex-row">
        <Input
          key={documento}
          id="busca-documento"
          name="documento"
          defaultValue={documento}
          placeholder="000.000.000-00 ou 00.000.000/0000-00"
          autoComplete="off"
          autoFocus
          aria-invalid={falha || undefined}
          className="h-10 text-base sm:max-w-md"
        />
        <Button type="submit" size="lg" className="h-10">
          <Search />
          Buscar
        </Button>
      </div>
      <p className="text-xs text-muted-foreground">Com ou sem pontuação.</p>
    </Form>
  );
}

function ClienteEncontrado({ cliente }: { cliente: ClienteDto }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center gap-2 text-lg">
          {cliente.nome}
          <SituacaoDoCliente ativo={cliente.ativo} />
        </CardTitle>
        <CardDescription>
          {cliente.ativo ? "Cliente encontrado." : "Cliente Inativo: não pode ser usado em novas operações."}
        </CardDescription>
        <CardAction>
          <Button asChild>
            <Link href={enderecoDoCliente(cliente.id)}>
              Abrir Cliente
              <ArrowRight />
            </Link>
          </Button>
        </CardAction>
      </CardHeader>
      <CardContent>
        <DadosDoCliente cliente={cliente} />
      </CardContent>
    </Card>
  );
}

function CadastroNoBalcao({ documento }: { documento: string }) {
  return (
    <div className="flex flex-col gap-4">
      <Alert>
        <UserPlus />
        <AlertTitle>Nenhum Cliente com este documento</AlertTitle>
        <AlertDescription>Cadastre o Cliente agora. O documento já está preenchido.</AlertDescription>
      </Alert>
      <Card>
        <CardHeader>
          <CardTitle>Cadastrar Cliente</CardTitle>
        </CardHeader>
        <CardContent>
          <FormularioDeCliente
            key={documento}
            acao={cadastrarCliente}
            valoresIniciais={{ documento }}
            comDocumento
            rotuloDoEnvio="Cadastrar Cliente"
          />
        </CardContent>
      </Card>
    </div>
  );
}

function ResultadoDaBusca({ documento, resultado }: { documento: string; resultado: Resultado<ClienteDto[]> }) {
  if (!resultado.ok) {
    return <AvisoDeFalha falha={resultado.falha} />;
  }
  const [cliente] = resultado.dados;
  return cliente ? <ClienteEncontrado cliente={cliente} /> : <CadastroNoBalcao documento={documento} />;
}

export default async function Balcao({ searchParams }: PageProps<"/administrativo/balcao">) {
  await exigirTela("balcao");
  const parametro = (await searchParams).documento;
  const documento = (typeof parametro === "string" ? parametro : "").trim();

  let resultado: Resultado<ClienteDto[]> | null = null;
  if (documento) {
    const api = await apiAdministrativa();
    resultado = await resultadoDaApi<ClienteDto[]>(
      api.GET("/api/administrativo/clientes", { params: { query: { documento } } }),
    );
  }

  return (
    <div className="flex max-w-4xl flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold">Balcão</h1>
        <p className="text-sm text-muted-foreground">Identifique o Cliente pelo documento para começar o atendimento.</p>
      </div>
      <BuscaPorDocumento documento={documento} falha={resultado?.ok === false} />
      {resultado && <ResultadoDaBusca documento={documento} resultado={resultado} />}
    </div>
  );
}
