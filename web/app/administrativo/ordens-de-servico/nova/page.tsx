import { ArrowLeft } from "lucide-react";
import Link from "next/link";
import type { ReactNode } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto, ServicoDto, VeiculoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDocumento } from "@/lib/formatacao";
import { FormularioDeAbertura } from "./formulario-de-abertura";

function Moldura({ enderecoDeVolta, children }: { enderecoDeVolta: string; children: ReactNode }) {
  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <Button asChild variant="ghost" size="sm" className="self-start">
        <Link href={enderecoDeVolta}>
          <ArrowLeft />
          {enderecoDeVolta === "/administrativo/balcao" ? "Balcão" : "Cliente"}
        </Link>
      </Button>
      <h1 className="text-2xl font-semibold">Abrir OS</h1>
      {children}
    </div>
  );
}

function Impedimento({ titulo, mensagem }: { titulo: string; mensagem: string }) {
  return (
    <Alert variant="destructive">
      <AlertTitle>{titulo}</AlertTitle>
      <AlertDescription>{mensagem}</AlertDescription>
    </Alert>
  );
}

export default async function AbrirOsDoVeiculo({ searchParams }: PageProps<"/administrativo/ordens-de-servico/nova">) {
  await exigirTela("ordensDeServico");
  const { veiculoId } = await searchParams;
  if (typeof veiculoId !== "string" || !veiculoId) {
    return (
      <Moldura enderecoDeVolta="/administrativo/balcao">
        <Impedimento
          titulo="Veículo não informado"
          mensagem="A OS é aberta a partir de um Veículo do Cliente. Busque o Cliente no Balcão e use Abrir OS no Veículo."
        />
      </Moldura>
    );
  }

  const api = await apiAdministrativa();
  const veiculo = await resultadoDaApi<VeiculoDto>(
    api.GET("/api/administrativo/veiculos/{id}", { params: { path: { id: veiculoId } } }),
  );
  if (!veiculo.ok) {
    return (
      <Moldura enderecoDeVolta="/administrativo/balcao">
        <AvisoDeFalha falha={veiculo.falha} />
      </Moldura>
    );
  }

  const enderecoDoCliente = `/administrativo/clientes/${veiculo.dados.proprietarioId}`;
  const [cliente, servicos] = await Promise.all([
    resultadoDaApi<ClienteDto>(
      api.GET("/api/administrativo/clientes/{id}", { params: { path: { id: veiculo.dados.proprietarioId } } }),
    ),
    resultadoDaApi<ServicoDto[]>(api.GET("/api/administrativo/servicos")),
  ]);
  if (!cliente.ok || !servicos.ok) {
    return (
      <Moldura enderecoDeVolta={enderecoDoCliente}>
        {!cliente.ok && <AvisoDeFalha falha={cliente.falha} />}
        {!servicos.ok && <AvisoDeFalha falha={servicos.falha} />}
      </Moldura>
    );
  }

  const servicosAtivos = servicos.dados.filter((servico) => servico.ativo);
  const impedimento = !cliente.dados.ativo
    ? { titulo: "Cliente Inativo", mensagem: "Um Cliente Inativo não pode ter uma nova OS." }
    : !veiculo.dados.ativo
      ? { titulo: "Veículo Inativo", mensagem: "Um Veículo Inativo não pode ter uma nova OS." }
      : servicosAtivos.length === 0
        ? {
            titulo: "Nenhum Serviço ativo",
            mensagem: "A OS precisa de ao menos um Serviço. Peça ao Gerente para cadastrar ou revisar o catálogo.",
          }
        : null;

  return (
    <Moldura enderecoDeVolta={enderecoDoCliente}>
      <Card>
        <CardHeader>
          <CardTitle>Atendimento</CardTitle>
          <CardDescription>A OS nasce Recebida para este Cliente e Veículo.</CardDescription>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-3 sm:grid-cols-2">
            <div className="flex min-w-0 flex-col gap-1">
              <dt className="text-xs text-muted-foreground">Cliente</dt>
              <dd className="text-sm font-medium">
                {cliente.dados.nome}{" "}
                <span className="font-normal text-muted-foreground">
                  {formatarDocumento(cliente.dados.tipoDocumento, cliente.dados.documento)}
                </span>
              </dd>
            </div>
            <div className="flex min-w-0 flex-col gap-1">
              <dt className="text-xs text-muted-foreground">Veículo</dt>
              <dd className="text-sm font-medium">
                <span className="font-mono">{veiculo.dados.placa}</span>{" "}
                <span className="font-normal text-muted-foreground">
                  {veiculo.dados.marca} {veiculo.dados.modelo} {veiculo.dados.anoFabricacao}
                </span>
              </dd>
            </div>
          </dl>
        </CardContent>
      </Card>

      {impedimento ? (
        <Impedimento titulo={impedimento.titulo} mensagem={impedimento.mensagem} />
      ) : (
        <FormularioDeAbertura
          clienteId={cliente.dados.id}
          veiculoId={veiculo.dados.id}
          servicos={servicosAtivos}
          enderecoDeVolta={enderecoDoCliente}
        />
      )}
    </Moldura>
  );
}
