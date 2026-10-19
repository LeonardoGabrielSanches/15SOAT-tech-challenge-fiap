import "server-only";
import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import { resultadoDaApi, type Resultado } from "@/lib/api/resultado";
import { exigirPapel } from "@/lib/autenticacao/sessao";
import { enderecoDaOs } from "../../endereco-da-os";

type Api = Awaited<ReturnType<typeof apiAdministrativa>>;

type ChamadaDaApi = Parameters<typeof resultadoDaApi>[0];

export async function executarNaOs(osId: string, chamada: (api: Api) => ChamadaDaApi): Promise<Resultado<undefined>> {
  await exigirPapel();
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi(chamada(api));
  if (resultado.ok) {
    revalidatePath(enderecoDaOs(osId));
  }
  return resultado;
}
