import { apiAdministrativa } from "@/lib/api/cliente";
import type { ServicoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { InclusaoDeServico } from "./inclusao-de-servico";
import type { PropsDaAcaoDaOs } from "./tipos";

export async function IncluirServico({ os }: PropsDaAcaoDaOs) {
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ServicoDto[]>(api.GET("/api/administrativo/servicos"));
  if (!resultado.ok) {
    return <InclusaoDeServico osId={os.id} servicosDisponiveis={[]} falhaAoCarregar={resultado.falha} />;
  }
  const naOs = new Set(os.servicos.map((servico) => servico.servicoId));
  const servicosDisponiveis = resultado.dados.filter((servico) => servico.ativo && !naOs.has(servico.id));
  return <InclusaoDeServico osId={os.id} servicosDisponiveis={servicosDisponiveis} />;
}
