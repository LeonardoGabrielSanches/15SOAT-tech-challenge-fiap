import type { ConsultaPublicaDaOsDto } from "@/lib/api/dtos";
import type { Falha } from "@/lib/api/resultado";
import type { DecisaoConfirmada, FalhaDaDecisao } from "../aprovacao/decisao-do-cliente";

export type ConsultaInformada = { numero: string; documento: string };

export type LinkDoOrcamento = { numero: string; token: string };

export type EstadoDaConsulta =
  | { situacao: "inicial" }
  | {
      situacao: "encontrada";
      os: ConsultaPublicaDaOsDto;
      consulta: ConsultaInformada;
      decisaoConfirmada?: DecisaoConfirmada;
      falhaDaDecisao?: FalhaDaDecisao;
    }
  | { situacao: "naoEncontrada" }
  | { situacao: "falha"; falha: Falha };

export const estadoInicial: EstadoDaConsulta = { situacao: "inicial" };

export function ehDoLink(numeroDaOs: string, numeroDoLink: string): boolean {
  return numeroDaOs.trim().toUpperCase() === numeroDoLink.trim().toUpperCase();
}
