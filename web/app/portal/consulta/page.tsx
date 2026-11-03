import type { Metadata } from "next";
import { atenderConsulta } from "./acoes";
import { FormularioDeConsulta } from "./formulario-de-consulta";

export const metadata: Metadata = {
  title: "Consultar OS | Oficina",
  referrer: "no-referrer",
};

function lerParametro(valor: string | string[] | undefined): string {
  return typeof valor === "string" ? valor.trim() : "";
}

export default async function ConsultaDaOs({ searchParams }: PageProps<"/portal/consulta">) {
  const parametros = await searchParams;
  const numero = lerParametro(parametros.numero);
  const token = lerParametro(parametros.token);
  const link = numero && token ? { numero, token } : null;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-xl font-semibold">Consultar OS</h1>
        <p className="text-sm text-muted-foreground">
          Informe o Número da OS e o seu CPF/CNPJ para acompanhar o Status e o orçamento.
        </p>
      </div>
      <FormularioDeConsulta
        numeroInicial={numero}
        numeroDoLink={link?.numero ?? null}
        atender={atenderConsulta.bind(null, link)}
      />
    </div>
  );
}
