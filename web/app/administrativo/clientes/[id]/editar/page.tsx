import { ArrowLeft } from "lucide-react";
import Link from "next/link";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDocumento, nomeDoTipoDeDocumento } from "@/lib/formatacao";
import { atualizarCliente } from "../../acoes";
import { enderecoDoCliente } from "../../endereco-do-cliente";
import { FormularioDeCliente } from "../../formulario-de-cliente";

export default async function EditarCliente({ params }: PageProps<"/administrativo/clientes/[id]/editar">) {
  await exigirTela("clientes");
  const { id } = await params;
  const paginaDoCliente = enderecoDoCliente(id);
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ClienteDto>(
    api.GET("/api/administrativo/clientes/{id}", { params: { path: { id } } }),
  );

  return (
    <div className="flex max-w-4xl flex-col gap-6">
      <Button asChild variant="ghost" size="sm" className="self-start">
        <Link href={paginaDoCliente}>
          <ArrowLeft />
          Voltar ao Cliente
        </Link>
      </Button>
      {!resultado.ok ? (
        <AvisoDeFalha falha={resultado.falha} />
      ) : (
        <>
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold">Editar Cliente</h1>
            <p className="text-sm text-muted-foreground">
              {nomeDoTipoDeDocumento[resultado.dados.tipoDocumento]}{" "}
              {formatarDocumento(resultado.dados.tipoDocumento, resultado.dados.documento)}. O documento não pode ser
              alterado.
            </p>
          </div>
          {resultado.dados.ativo ? (
            <FormularioDeCliente
              acao={atualizarCliente.bind(null, id)}
              valoresIniciais={{
                nome: resultado.dados.nome,
                documento: resultado.dados.documento,
                email: resultado.dados.email,
                telefone: resultado.dados.telefone ?? "",
              }}
              comDocumento={false}
              rotuloDoEnvio="Salvar alterações"
              enderecoDeCancelamento={paginaDoCliente}
            />
          ) : (
            <Alert variant="destructive">
              <AlertTitle>Cliente Inativo</AlertTitle>
              <AlertDescription>Um Cliente Inativo não pode ser editado.</AlertDescription>
            </Alert>
          )}
        </>
      )}
    </div>
  );
}
