"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ServicoDto } from "@/lib/api/dtos";
import { dadosInvalidos, resultadoDaApi, type Resultado } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { lerTexto } from "@/lib/dados-do-formulario";
import { lerDinheiro } from "@/lib/formatacao";

const enderecoDaTela = "/administrativo/servicos";

export async function salvarServico(dados: FormData): Promise<Resultado<ServicoDto>> {
  await exigirTela("servicos");
  const id = lerTexto(dados, "id");
  const preco = lerDinheiro(lerTexto(dados, "preco"));
  if (preco === null) {
    return dadosInvalidos("Informe o preço em reais, por exemplo 120,50.");
  }
  const corpo = { nome: lerTexto(dados, "nome"), descricao: lerTexto(dados, "descricao") || null, preco };
  const api = await apiAdministrativa();
  const resultado = id
    ? await resultadoDaApi<ServicoDto>(
        api.PUT("/api/administrativo/servicos/{id}", { params: { path: { id } }, body: corpo }),
      )
    : await resultadoDaApi<ServicoDto>(api.POST("/api/administrativo/servicos", { body: corpo }));
  if (resultado.ok) {
    revalidatePath(enderecoDaTela);
  }
  return resultado;
}

export async function inativarServico(id: string): Promise<Resultado<undefined>> {
  await exigirTela("servicos");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi(
    api.DELETE("/api/administrativo/servicos/{id}", { params: { path: { id } } }),
  );
  if (resultado.ok) {
    revalidatePath(enderecoDaTela);
  }
  return resultado;
}
