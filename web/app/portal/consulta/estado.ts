import type { ConsultaPublicaDaOsDto } from "@/lib/api/dtos";
import type { Falha } from "@/lib/api/resultado";

export type EstadoDaConsulta =
  | { situacao: "inicial" }
  | { situacao: "encontrada"; os: ConsultaPublicaDaOsDto }
  | { situacao: "naoEncontrada" }
  | { situacao: "falha"; falha: Falha };

export const estadoInicial: EstadoDaConsulta = { situacao: "inicial" };
