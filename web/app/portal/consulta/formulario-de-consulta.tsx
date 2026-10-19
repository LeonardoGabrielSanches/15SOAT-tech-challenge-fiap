"use client";

import { CircleAlert, Search } from "lucide-react";
import { useActionState, useState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { consultarOs } from "./acoes";
import { estadoInicial } from "./estado";
import { ResultadoDaConsulta } from "./resultado-da-consulta";

export function FormularioDeConsulta() {
  const [estado, consultar, consultando] = useActionState(consultarOs, estadoInicial);
  const [numero, setNumero] = useState("");
  const [documento, setDocumento] = useState("");

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardContent>
          <form action={consultar} className="grid gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
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
                value={documento}
                onChange={(evento) => setDocumento(evento.target.value)}
              />
            </div>
            <Button type="submit" disabled={consultando}>
              <Search />
              {consultando ? "Consultando..." : "Consultar"}
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
      {estado.situacao === "encontrada" && <ResultadoDaConsulta os={estado.os} />}
    </div>
  );
}
