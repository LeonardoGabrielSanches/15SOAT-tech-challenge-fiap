import { ArrowLeft, CircleCheck } from "lucide-react";
import Link from "next/link";
import type { ReactNode } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { OrdemDeServicoDetalheDto } from "@/lib/api/dtos";
import { resultadoDaApi, type Falha } from "@/lib/api/resultado";
import { exigirPapel } from "@/lib/autenticacao/sessao";
import { podeAcessarTela, tela, telaInicial, type Papel } from "@/lib/capacidades";
import { formatarDataHora, formatarDocumento, formatarTelefone } from "@/lib/formatacao";
import { enderecoDaOs } from "../endereco-da-os";
import { StatusDaOs } from "../status-da-os";
import { AcoesDeStatusDaOs } from "./acoes/acoes-da-os";
import { CopiarLink } from "./copiar-link";
import { HistoricoDeStatus } from "./historico-de-status";
import { PecasInsumosDaOs } from "./pecas-insumos-da-os";
import { ServicosDaOs } from "./servicos-da-os";
import { VersoesDoOrcamento } from "./versoes-do-orcamento";

function Voltar({ papel }: { papel: Papel }) {
  const destino = podeAcessarTela(papel, "ordensDeServico") ? tela("ordensDeServico") : telaInicial(papel);
  return (
    <Button asChild variant="ghost" size="sm" className="self-start">
      <Link href={destino.href}>
        <ArrowLeft />
        {destino.titulo}
      </Link>
    </Button>
  );
}

function falhaDaOs(falha: Falha): Falha {
  if (falha.status === 404) {
    return {
      ...falha,
      titulo: "OS não encontrada",
      mensagem: "Não existe OS com este identificador. Confira o link ou o identificador recebido.",
    };
  }
  if (falha.status === 403) {
    return { ...falha, mensagem: "O seu papel não permite ver esta OS." };
  }
  return falha;
}

function Dado({ rotulo, children }: { rotulo: string; children: ReactNode }) {
  return (
    <div className="flex min-w-0 flex-col gap-1">
      <dt className="text-xs text-muted-foreground">{rotulo}</dt>
      <dd className="text-sm font-medium break-words">{children}</dd>
    </div>
  );
}

function ClienteEVeiculo({ os, papel }: { os: OrdemDeServicoDetalheDto; papel: Papel }) {
  const { cliente, veiculo } = os;
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle>Cliente</CardTitle>
          {podeAcessarTela(papel, "clientes") && (
            <CardAction>
              <Button asChild variant="outline" size="sm">
                <Link href={`/administrativo/clientes/${cliente.id}`}>Abrir Cliente</Link>
              </Button>
            </CardAction>
          )}
        </CardHeader>
        <CardContent>
          <dl className="grid gap-3 sm:grid-cols-2">
            <Dado rotulo="Nome">{cliente.nome}</Dado>
            <Dado rotulo={cliente.tipoDocumento === "Cpf" ? "CPF" : "CNPJ"}>
              {formatarDocumento(cliente.tipoDocumento, cliente.documento)}
            </Dado>
            <Dado rotulo="E-mail">{cliente.email}</Dado>
            <Dado rotulo="Telefone">{cliente.telefone ? formatarTelefone(cliente.telefone) : "Não informado"}</Dado>
          </dl>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Veículo</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-3 sm:grid-cols-2">
            <Dado rotulo="Placa">
              <span className="font-mono">{veiculo.placa}</span>
            </Dado>
            <Dado rotulo="Ano">{veiculo.anoFabricacao}</Dado>
            <Dado rotulo="Marca">{veiculo.marca}</Dado>
            <Dado rotulo="Modelo">{veiculo.modelo}</Dado>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}

export default async function PaginaDaOs({ params, searchParams }: PageProps<"/administrativo/ordens-de-servico/[id]">) {
  const { papel } = await exigirPapel();
  const { id } = await params;
  const { aberta } = await searchParams;
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<OrdemDeServicoDetalheDto>(
    api.GET("/api/administrativo/ordens-de-servico/{id}", { params: { path: { id } } }),
  );

  if (!resultado.ok) {
    return (
      <div className="flex max-w-5xl flex-col gap-4">
        <Voltar papel={papel} />
        <AvisoDeFalha falha={falhaDaOs(resultado.falha)} />
      </div>
    );
  }

  const os = resultado.dados;

  return (
    <div className="flex max-w-5xl flex-col gap-6">
      <Voltar papel={papel} />

      {aberta === "1" && (
        <Alert>
          <CircleCheck />
          <AlertTitle>OS Recebida</AlertTitle>
          <AlertDescription>Informe ao Cliente o Número da OS {os.numero} para que ele acompanhe o atendimento.</AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardDescription>Número da OS</CardDescription>
          <CardTitle>
            <h1 className="font-mono text-3xl font-semibold tracking-tight break-all sm:text-4xl">{os.numero}</h1>
          </CardTitle>
          <CardAction>
            <CopiarLink caminho={enderecoDaOs(os.id)} />
          </CardAction>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm">
            <StatusDaOs status={os.status} />
            <span className="text-muted-foreground">Aberta em {formatarDataHora(os.abertaEm)}</span>
            <span className="text-muted-foreground">
              {os.veiculo.placa} · {os.cliente.nome}
            </span>
          </div>
          <AcoesDeStatusDaOs os={os} papel={papel} />
        </CardContent>
      </Card>

      {os.status === "EmDiagnostico" && os.motivoDaUltimaRecusa && (
        <Alert>
          <AlertTitle>Orçamento recusado pelo Cliente</AlertTitle>
          <AlertDescription>Motivo informado: {os.motivoDaUltimaRecusa}</AlertDescription>
        </Alert>
      )}

      <ClienteEVeiculo os={os} papel={papel} />
      <ServicosDaOs os={os} papel={papel} />
      <PecasInsumosDaOs os={os} papel={papel} />
      <VersoesDoOrcamento versoes={os.versoesDoOrcamento} />
      <HistoricoDeStatus historico={os.historico} />
    </div>
  );
}
