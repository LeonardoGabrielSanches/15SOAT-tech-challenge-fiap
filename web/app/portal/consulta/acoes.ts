"use server";

import { apiPublica } from "@/lib/api/cliente";
import type { ConsultaPublicaDaOsDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import type { EstadoDaConsulta } from "./estado";

function texto(formData: FormData, campo: string): string {
  const valor = formData.get(campo);
  return typeof valor === "string" ? valor.trim() : "";
}

export async function consultarOs(_: EstadoDaConsulta, formData: FormData): Promise<EstadoDaConsulta> {
  const numero = texto(formData, "numero");
  const documento = texto(formData, "documento");
  if (!numero || !documento) {
    return { situacao: "naoEncontrada" };
  }
  const resultado = await resultadoDaApi<ConsultaPublicaDaOsDto>(
    apiPublica().POST("/api/publico/ordens-de-servico/consulta", { body: { numero, documento } }),
  );
  if (resultado.ok) {
    return { situacao: "encontrada", os: resultado.dados };
  }
  if ([400, 404, 422].includes(resultado.falha.status)) {
    return { situacao: "naoEncontrada" };
  }
  return { situacao: "falha", falha: resultado.falha };
}
