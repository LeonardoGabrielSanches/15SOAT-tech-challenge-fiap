"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { PecaInsumoDto, SaldoDto, TipoPecaInsumo } from "@/lib/api/dtos";
import { resultadoDaApi, type Resultado } from "@/lib/api/resultado";
import { lerDinheiro, lerQuantidade } from "@/lib/formatacao";

const caminhoDaTela = "/administrativo/pecas-insumos";

function dadoInvalido<T>(mensagem: string): Resultado<T> {
  return { ok: false, falha: { status: 422, titulo: "Dados inválidos", mensagem } };
}

function texto(dados: FormData, campo: string): string {
  const valor = dados.get(campo);
  return typeof valor === "string" ? valor : "";
}

function lerTipo(dados: FormData): TipoPecaInsumo | null {
  const tipo = texto(dados, "tipo");
  return tipo === "Peca" || tipo === "Insumo" ? tipo : null;
}

export async function cadastrarPecaInsumo(dados: FormData): Promise<Resultado<PecaInsumoDto>> {
  const tipo = lerTipo(dados);
  if (!tipo) {
    return dadoInvalido("Escolha se é Peça ou Insumo.");
  }
  const preco = lerDinheiro(texto(dados, "preco"));
  if (preco === null) {
    return dadoInvalido("Informe o preço como número, por exemplo 35 ou 35,90.");
  }
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<PecaInsumoDto>(
    api.POST("/api/pecas-insumos", {
      body: { nome: texto(dados, "nome"), tipo, unidadeDeMedida: texto(dados, "unidadeDeMedida"), preco },
    }),
  );
  if (resultado.ok) {
    revalidatePath(caminhoDaTela);
  }
  return resultado;
}

export async function editarPecaInsumo(id: string, dados: FormData): Promise<Resultado<PecaInsumoDto>> {
  const preco = lerDinheiro(texto(dados, "preco"));
  if (preco === null) {
    return dadoInvalido("Informe o preço como número, por exemplo 35 ou 35,90.");
  }
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<PecaInsumoDto>(
    api.PUT("/api/pecas-insumos/{id}", {
      params: { path: { id } },
      body: { nome: texto(dados, "nome"), unidadeDeMedida: texto(dados, "unidadeDeMedida"), preco },
    }),
  );
  if (resultado.ok) {
    revalidatePath(caminhoDaTela);
  }
  return resultado;
}

export async function inativarPecaInsumo(id: string): Promise<Resultado<undefined>> {
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi(api.DELETE("/api/pecas-insumos/{id}", { params: { path: { id } } }));
  if (resultado.ok) {
    revalidatePath(caminhoDaTela);
  }
  return resultado;
}

export async function registrarEntradaDeMercadoria(id: string, dados: FormData): Promise<Resultado<SaldoDto>> {
  const quantidade = lerQuantidade(texto(dados, "quantidade"));
  if (quantidade === null) {
    return dadoInvalido("Informe a quantidade como número, por exemplo 10 ou 2,5.");
  }
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<SaldoDto>(
    api.POST("/api/pecas-insumos/{id}/entradas", { params: { path: { id } }, body: { quantidade } }),
  );
  if (resultado.ok) {
    revalidatePath(caminhoDaTela);
  }
  return resultado;
}
