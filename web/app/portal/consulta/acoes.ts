"use server";

import { apiPublica } from "@/lib/api/cliente";
import type { ConsultaPublicaDaOsDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { lerTexto } from "@/lib/dados-do-formulario";
import { decidirOrcamento } from "../aprovacao/decisao";
import { confirmacaoDa } from "../aprovacao/decisao-do-cliente";
import { ehDoLink, type ConsultaInformada, type EstadoDaConsulta, type LinkDoOrcamento } from "./estado";

async function consultar(consulta: ConsultaInformada): Promise<EstadoDaConsulta> {
  const resultado = await resultadoDaApi<ConsultaPublicaDaOsDto>(
    apiPublica().POST("/api/publico/ordens-de-servico/consulta", { body: consulta }),
  );
  if (resultado.ok) {
    return { situacao: "encontrada", os: resultado.dados, consulta };
  }
  if ([400, 404, 422].includes(resultado.falha.status)) {
    return { situacao: "naoEncontrada" };
  }
  return { situacao: "falha", falha: resultado.falha };
}

async function consultarOs(formData: FormData): Promise<EstadoDaConsulta> {
  const numero = lerTexto(formData, "numero");
  const documento = lerTexto(formData, "documento");
  if (!numero || !documento) {
    return { situacao: "naoEncontrada" };
  }
  return consultar({ numero, documento });
}

async function decidirNaConsulta(
  link: LinkDoOrcamento,
  estado: EstadoDaConsulta,
  formData: FormData,
): Promise<EstadoDaConsulta> {
  if (estado.situacao !== "encontrada" || !ehDoLink(estado.os.numero, link.numero)) {
    return estado;
  }
  const desfecho = await decidirOrcamento(link.token, formData);
  if (!desfecho.ok) {
    return { ...estado, falhaDaDecisao: desfecho.falha };
  }
  const decisaoConfirmada = confirmacaoDa[desfecho.decisao];
  const atualizada = await consultar(estado.consulta);
  if (atualizada.situacao === "encontrada") {
    return { ...atualizada, decisaoConfirmada };
  }
  return { ...estado, decisaoConfirmada, falhaDaDecisao: undefined };
}

export async function atenderConsulta(
  link: LinkDoOrcamento | null,
  estado: EstadoDaConsulta,
  formData: FormData,
): Promise<EstadoDaConsulta> {
  if (!formData.has("decisao")) {
    return consultarOs(formData);
  }
  return link ? decidirNaConsulta(link, estado, formData) : estado;
}
