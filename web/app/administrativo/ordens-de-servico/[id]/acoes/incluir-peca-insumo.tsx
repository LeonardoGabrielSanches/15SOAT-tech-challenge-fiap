import { apiAdministrativa } from "@/lib/api/cliente";
import type { PecaInsumoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { FormularioDePecaInsumoNaOs } from "./formulario-de-peca-insumo-na-os";
import type { PropsDaAcaoDaOs } from "./tipos";

export async function IncluirPecaInsumo({ os }: PropsDaAcaoDaOs) {
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<PecaInsumoDto[]>(
    api.GET("/api/pecas-insumos", { params: { query: { incluirInativos: false } } }),
  );
  const quantidadesNaOs = Object.fromEntries(os.pecasInsumos.map((item) => [item.pecaInsumoId, item.quantidade]));

  return (
    <FormularioDePecaInsumoNaOs
      osId={os.id}
      opcoes={resultado.ok ? resultado.dados.filter((pecaInsumo) => pecaInsumo.ativo) : []}
      falhaAoCarregar={resultado.ok ? null : resultado.falha}
      quantidadesNaOs={quantidadesNaOs}
    />
  );
}
