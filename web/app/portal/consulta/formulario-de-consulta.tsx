"use client";

import { CircleAlert, Search } from "lucide-react";
import { useActionState, useState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { confirmacoes, impedeNovaDecisao } from "../aprovacao/decisao-do-cliente";
import { FormularioDaDecisao } from "../aprovacao/formulario-da-decisao";
import { ehDoLink, estadoInicial, type EstadoDaConsulta } from "./estado";
import { ResultadoDaConsulta } from "./resultado-da-consulta";

type Props = {
  numeroInicial: string;
  numeroDoLink: string | null;
  atender: (estado: EstadoDaConsulta, formData: FormData) => Promise<EstadoDaConsulta>;
};

type ConsultaEncontrada = Extract<EstadoDaConsulta, { situacao: "encontrada" }>;

function DecisaoNaConsulta({
  estado,
  acao,
  enviando,
}: {
  estado: ConsultaEncontrada;
  acao: (formData: FormData) => void;
  enviando: boolean;
}) {
  const falha = estado.falhaDaDecisao?.falha;
  if (falha && impedeNovaDecisao(falha)) {
    return <AvisoDeFalha falha={falha} />;
  }
  return (
    <div className="flex flex-col gap-4">
      {falha && <AvisoDeFalha falha={falha} />}
      <FormularioDaDecisao acao={acao} enviando={enviando} erroDoMotivo={estado.falhaDaDecisao?.erroDoMotivo} />
    </div>
  );
}

function ConfirmacaoDaDecisao({ estado }: { estado: ConsultaEncontrada }) {
  if (!estado.decisaoConfirmada) {
    return null;
  }
  const { Icone, titulo, descricao } = confirmacoes[estado.decisaoConfirmada];
  return (
    <Alert>
      <Icone />
      <AlertTitle>{titulo}</AlertTitle>
      <AlertDescription>{descricao}</AlertDescription>
    </Alert>
  );
}

export function FormularioDeConsulta({ numeroInicial, numeroDoLink, atender }: Props) {
  const [estado, executar, executando] = useActionState(atender, estadoInicial);
  const [numero, setNumero] = useState(numeroInicial);
  const [documento, setDocumento] = useState("");

  const podeDecidir =
    estado.situacao === "encontrada" &&
    numeroDoLink !== null &&
    ehDoLink(estado.os.numero, numeroDoLink) &&
    estado.os.status === "AguardandoAprovacao" &&
    !estado.decisaoConfirmada;

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardContent>
          <form action={executar} className="grid gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
            <div className="flex flex-col gap-2">
              <Label htmlFor="numero">Número da OS</Label>
              <Input
                id="numero"
                name="numero"
                placeholder="OS-2026-00001"
                autoComplete="off"
                autoCapitalize="characters"
                required
                value={numero}
                onChange={(evento) => setNumero(evento.target.value)}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="documento">CPF/CNPJ</Label>
              <Input
                id="documento"
                name="documento"
                placeholder="Com ou sem pontuação"
                autoComplete="off"
                required
                autoFocus={numeroInicial !== ""}
                value={documento}
                onChange={(evento) => setDocumento(evento.target.value)}
              />
            </div>
            <Button type="submit" disabled={executando}>
              <Search />
              {executando ? "Aguarde..." : "Consultar"}
            </Button>
          </form>
        </CardContent>
      </Card>

      {estado.situacao === "naoEncontrada" && (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertTitle>OS não encontrada</AlertTitle>
          <AlertDescription>
            Não encontramos uma OS com este Número e CPF/CNPJ. Confira os dados e tente novamente.
          </AlertDescription>
        </Alert>
      )}
      {estado.situacao === "falha" && <AvisoDeFalha falha={estado.falha} />}
      {estado.situacao === "encontrada" && (
        <>
          <ConfirmacaoDaDecisao estado={estado} />
          <ResultadoDaConsulta
            os={estado.os}
            decisao={
              podeDecidir || estado.falhaDaDecisao ? (
                <DecisaoNaConsulta estado={estado} acao={executar} enviando={executando} />
              ) : null
            }
          />
        </>
      )}
    </div>
  );
}
