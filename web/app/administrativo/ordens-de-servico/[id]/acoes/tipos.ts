import type { ReactNode } from "react";
import type { OrdemDeServicoDetalheDto, PecaInsumoDaOsDto, ServicoDaOsDto } from "@/lib/api/dtos";
import type { Papel } from "@/lib/capacidades";

export type PropsDaAcaoDaOs = {
  os: OrdemDeServicoDetalheDto;
  papel: Papel;
};

export type PropsDaAcaoNoServico = PropsDaAcaoDaOs & { servico: ServicoDaOsDto };

export type PropsDaAcaoNaPecaInsumo = PropsDaAcaoDaOs & { pecaInsumo: PecaInsumoDaOsDto };

export type ComponenteDaAcao<P> = (props: P) => ReactNode | Promise<ReactNode>;
