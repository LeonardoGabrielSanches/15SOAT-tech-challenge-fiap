"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { PecaInsumoDto, SaldoDto, TipoPecaInsumo } from "@/lib/api/dtos";
import { dadosInvalidos, falhaDaResposta, resultadoDaApi, type Falha, type Resultado } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { podeGerirPecasInsumos } from "@/lib/capacidades";
import { lerTexto } from "@/lib/dados-do-formulario";
import { lerDinheiro, lerQuantidade } from "@/lib/formatacao";

const caminhoDaTela = "/administrativo/pecas-insumos";

async function recusaSemGestao(): Promise<{ ok: false; falha: Falha } | null> {
  const { papel } = await exigirTela("pecasInsumos");
  return podeGerirPecasInsumos(papel) ? null : { ok: false, falha: falhaDaResposta(403, undefined) };
}

function lerTipo(dados: FormData): TipoPecaInsumo | null {
  const tipo = lerTexto(dados, "tipo");
  return tipo === "Peca" || tipo === "Insumo" ? tipo : null;
}

export async function cadastrarPecaInsumo(dados: FormData): Promise<Resultado<PecaInsumoDto>> {
  const recusa = await recusaSemGestao();
  if (recusa) {
    return recusa;
  }
  const tipo = lerTipo(dados);
  if (!tipo) {
    return dadosInvalidos("Escolha se é Peça ou Insumo.");
  }
  const preco = lerDinheiro(lerTexto(dados, "preco"));
  if (preco === null) {
    return dadosInvalidos("Informe o preço como número, por exemplo 35 ou 35,90.");
  }
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<PecaInsumoDto>(
    api.POST("/api/pecas-insumos", {
      body: { nome: lerTexto(dados, "nome"), tipo, unidadeDeMedida: lerTexto(dados, "unidadeDeMedida"), preco },
    }),
  );
  if (resultado.ok) {
    revalidatePath(caminhoDaTela);
  }
  return resultado;
}

export async function editarPecaInsumo(id: string, dados: FormData): Promise<Resultado<PecaInsumoDto>> {
  const recusa = await recusaSemGestao();
  if (recusa) {
    return recusa;
  }
  const preco = lerDinheiro(lerTexto(dados, "preco"));
  if (preco === null) {
    return dadosInvalidos("Informe o preço como número, por exemplo 35 ou 35,90.");
  }
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<PecaInsumoDto>(
    api.PUT("/api/pecas-insumos/{id}", {
      params: { path: { id } },
      body: { nome: lerTexto(dados, "nome"), unidadeDeMedida: lerTexto(dados, "unidadeDeMedida"), preco },
    }),
  );
  if (resultado.ok) {
    revalidatePath(caminhoDaTela);
  }
  return resultado;
}

export async function inativarPecaInsumo(id: string): Promise<Resultado<undefined>> {
  const recusa = await recusaSemGestao();
  if (recusa) {
    return recusa;
  }
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi(api.DELETE("/api/pecas-insumos/{id}", { params: { path: { id } } }));
  if (resultado.ok) {
    revalidatePath(caminhoDaTela);
  }
  return resultado;
}

export async function registrarEntradaDeMercadoria(id: string, dados: FormData): Promise<Resultado<SaldoDto>> {
  const recusa = await recusaSemGestao();
  if (recusa) {
    return recusa;
  }
  const quantidade = lerQuantidade(lerTexto(dados, "quantidade"));
  if (quantidade === null) {
    return dadosInvalidos("Informe a quantidade como número, por exemplo 10 ou 2,5.");
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
