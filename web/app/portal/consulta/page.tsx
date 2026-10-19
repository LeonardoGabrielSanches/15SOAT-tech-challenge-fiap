import type { Metadata } from "next";
import { FormularioDeConsulta } from "./formulario-de-consulta";

export const metadata: Metadata = {
  title: "Consultar OS | Oficina",
};

export default function ConsultaDaOs() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-xl font-semibold">Consultar OS</h1>
        <p className="text-sm text-muted-foreground">
          Informe o Número da OS e o seu CPF/CNPJ para acompanhar o Status e o orçamento.
        </p>
      </div>
      <FormularioDeConsulta />
    </div>
  );
}
