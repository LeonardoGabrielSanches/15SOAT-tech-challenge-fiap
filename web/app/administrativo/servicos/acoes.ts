"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ServicoDto } from "@/lib/api/dtos";
import { resultadoDaApi, type Resultado } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
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

export type EstadoDaInativacao = Resultado<undefined> | null;

const enderecoDaTela = "/administrativo/servicos";

function texto(formData: FormData, campo: string): string {
  const valor = formData.get(campo);
  return typeof valor === "string" ? valor : "";
}

export async function salvarServico(
  _estadoAnterior: EstadoDoFormularioDeServico,
  formData: FormData,
): Promise<EstadoDoFormularioDeServico> {
  await exigirTela("servicos");
  const id = texto(formData, "id");
  const valores: ValoresDoServico = {
    nome: texto(formData, "nome"),
    descricao: texto(formData, "descricao"),
    preco: texto(formData, "preco"),
  };
  const preco = lerDinheiro(valores.preco);
  if (preco === null) {
    return {
      valores,
      resultado: {
        ok: false,
        falha: { status: 422, titulo: "Dados inválidos", mensagem: "Informe o preço em reais, por exemplo 120,50." },
      },
    };
  }
  const corpo = { nome: valores.nome.trim(), descricao: valores.descricao.trim() || null, preco };
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

export async function inativarServico(id: string): Promise<EstadoDaInativacao> {
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
