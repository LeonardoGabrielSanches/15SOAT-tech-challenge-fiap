"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ServicoDto } from "@/lib/api/dtos";
import { dadosInvalidos, resultadoDaApi, type Resultado } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { lerTexto } from "@/lib/dados-do-formulario";
import { lerDinheiro } from "@/lib/formatacao";

export type ValoresDoServico = {
  nome: string;
  descricao: string;
  preco: string;
};

export type EstadoDoFormularioDeServico = {
  resultado: Resultado<ServicoDto> | null;
  valores: ValoresDoServico;
};

const enderecoDaTela = "/administrativo/servicos";

export async function salvarServico(
  _estadoAnterior: EstadoDoFormularioDeServico,
  formData: FormData,
): Promise<EstadoDoFormularioDeServico> {
  await exigirTela("servicos");
  const id = lerTexto(formData, "id");
  const valores: ValoresDoServico = {
    nome: lerTexto(formData, "nome"),
    descricao: lerTexto(formData, "descricao"),
    preco: lerTexto(formData, "preco"),
  };
  const preco = lerDinheiro(valores.preco);
  if (preco === null) {
    return { valores, resultado: dadosInvalidos("Informe o preço em reais, por exemplo 120,50.") };
  }
  const corpo = { nome: valores.nome, descricao: valores.descricao || null, preco };
  const api = await apiAdministrativa();
  const resultado = id
    ? await resultadoDaApi<ServicoDto>(
        api.PUT("/api/administrativo/servicos/{id}", { params: { path: { id } }, body: corpo }),
      )
    : await resultadoDaApi<ServicoDto>(api.POST("/api/administrativo/servicos", { body: corpo }));
  if (resultado.ok) {
    revalidatePath(enderecoDaTela);
  }
  return { resultado, valores };
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
