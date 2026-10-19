"use client";

import Link from "next/link";
import { useActionState } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  campoDaFalha,
  estadoInicial,
  type CampoDoCliente,
  type EstadoDoFormularioDeCliente,
  type ValoresDoCliente,
} from "./formulario";

type Props = {
  acao: (estado: EstadoDoFormularioDeCliente, dados: FormData) => Promise<EstadoDoFormularioDeCliente>;
  valoresIniciais: Partial<ValoresDoCliente>;
  comDocumento: boolean;
  rotuloDoEnvio: string;
  enderecoDeCancelamento?: string;
};

type DefinicaoDoCampo = {
  campo: CampoDoCliente;
  rotulo: string;
  type: "text" | "email" | "tel";
  autoComplete: string;
  inputMode?: "text" | "email" | "tel" | "numeric";
};

const definicoesDosCampos: DefinicaoDoCampo[] = [
  { campo: "nome", rotulo: "Nome", type: "text", autoComplete: "name" },
  { campo: "documento", rotulo: "CPF ou CNPJ", type: "text", autoComplete: "off" },
  { campo: "email", rotulo: "E-mail", type: "email", autoComplete: "email", inputMode: "email" },
  { campo: "telefone", rotulo: "Telefone (opcional)", type: "tel", autoComplete: "tel", inputMode: "tel" },
];

export function FormularioDeCliente({
  acao,
  valoresIniciais,
  comDocumento,
  rotuloDoEnvio,
  enderecoDeCancelamento,
}: Props) {
  const [estado, enviar, enviando] = useActionState(acao, estadoInicial(valoresIniciais));
  const campoInvalido = campoDaFalha(estado.falha);
  const campos = definicoesDosCampos.filter((definicao) => comDocumento || definicao.campo !== "documento");

  return (
    <form key={estado.tentativa} action={enviar} noValidate className="flex max-w-xl flex-col gap-4">
      {estado.falha && <AvisoDeFalha falha={estado.falha} />}
      {campos.map(({ campo, rotulo, type, autoComplete, inputMode }) => (
        <div key={campo} className="flex flex-col gap-2">
          <Label htmlFor={`cliente-${campo}`}>{rotulo}</Label>
          <Input
            id={`cliente-${campo}`}
            name={campo}
            type={type}
            inputMode={inputMode}
            autoComplete={autoComplete}
            defaultValue={estado.valores[campo]}
            aria-invalid={campoInvalido === campo || undefined}
            autoFocus={campoInvalido === campo}
          />
        </div>
      ))}
      <div className="flex flex-wrap gap-2">
        <Button type="submit" disabled={enviando}>
          {enviando ? "Salvando..." : rotuloDoEnvio}
        </Button>
        {enderecoDeCancelamento && (
          <Button asChild variant="outline">
            <Link href={enderecoDeCancelamento}>Cancelar</Link>
          </Button>
        )}
      </div>
    </form>
  );
}
