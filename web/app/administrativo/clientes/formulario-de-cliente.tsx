"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import type { ClienteDto } from "@/lib/api/dtos";
import type { Resultado } from "@/lib/api/resultado";
import { enderecoDoCliente } from "./endereco-do-cliente";
import { campoDaFalha, type CampoDoCliente, type ValoresDoCliente } from "./formulario";

type Props = {
  acao: (dados: FormData) => Promise<Resultado<ClienteDto>>;
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
  const router = useRouter();
  const { falha, enviar, pendente } = useAcaoDeFormulario(
    (dados) => acao(dados),
    (cliente) => router.push(enderecoDoCliente(cliente.id)),
  );
  const campoInvalido = campoDaFalha(falha);
  const campos = definicoesDosCampos.filter((definicao) => comDocumento || definicao.campo !== "documento");

  useEffect(() => {
    if (campoInvalido) {
      document.getElementById(`cliente-${campoInvalido}`)?.focus();
    }
  }, [falha, campoInvalido]);

  return (
    <form onSubmit={enviar} noValidate className="flex max-w-xl flex-col gap-4">
      {falha && <AvisoDeFalha falha={falha} />}
      {campos.map(({ campo, rotulo, type, autoComplete, inputMode }) => (
        <div key={campo} className="flex flex-col gap-2">
          <Label htmlFor={`cliente-${campo}`}>{rotulo}</Label>
          <Input
            id={`cliente-${campo}`}
            name={campo}
            type={type}
            inputMode={inputMode}
            autoComplete={autoComplete}
            defaultValue={valoresIniciais[campo] ?? ""}
            aria-invalid={campoInvalido === campo || undefined}
          />
        </div>
      ))}
      <div className="flex flex-wrap gap-2">
        <Button type="submit" disabled={pendente}>
          {pendente ? "Salvando..." : rotuloDoEnvio}
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
