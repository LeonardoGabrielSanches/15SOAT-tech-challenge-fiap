"use server";

import { apiAdministrativa } from "@/lib/api/cliente";
import type { OrdemDeServicoDetalheDto } from "@/lib/api/dtos";
import { dadosInvalidos, resultadoDaApi, type Resultado } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { lerTexto } from "@/lib/dados-do-formulario";

const identificador = "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";
const identificadorNoLink = new RegExp(`ordens-de-servico/(${identificador})`, "i");
const identificadorSolto = new RegExp(identificador, "i");

function lerIdentificador(texto: string): string | null {
  const encontrado = texto.match(identificadorNoLink)?.[1] ?? texto.match(identificadorSolto)?.[0];
  return encontrado ? encontrado.toLowerCase() : null;
}

export async function localizarOs(dados: FormData): Promise<Resultado<{ id: string }>> {
  await exigirTela("abrirOs");
  const texto = lerTexto(dados, "referencia");
  if (!texto) {
    return dadosInvalidos("Cole o link da OS ou o identificador.");
  }
  const id = lerIdentificador(texto);
  if (!id) {
    return dadosInvalidos(
      "Cole o link copiado na página da OS ou o identificador que aparece no fim dele, no formato 3fa85f64-5717-4562-b3fc-2c963f66afa6. O Número da OS não serve aqui.",
      "Link ou identificador não reconhecido",
    );
  }
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<OrdemDeServicoDetalheDto>(
    api.GET("/api/administrativo/ordens-de-servico/{id}", { params: { path: { id } } }),
  );
  if (!resultado.ok) {
    if (resultado.falha.status === 404) {
      return {
        ok: false,
        falha: {
          ...resultado.falha,
          titulo: "OS não encontrada",
          mensagem: "Não existe OS com este identificador. Confira o link ou o identificador recebido.",
        },
      };
    }
    return resultado;
  }
  return { ok: true, dados: { id: resultado.dados.id } };
}
