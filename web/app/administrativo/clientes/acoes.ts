"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto } from "@/lib/api/dtos";
import { resultadoDaApi, type Resultado } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { lerTexto } from "@/lib/dados-do-formulario";
import { enderecoDoCliente } from "./endereco-do-cliente";

export async function cadastrarCliente(dados: FormData): Promise<Resultado<ClienteDto>> {
  await exigirTela("balcao");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ClienteDto>(
    api.POST("/api/administrativo/clientes", {
      body: {
        nome: lerTexto(dados, "nome"),
        documento: lerTexto(dados, "documento"),
        email: lerTexto(dados, "email"),
        telefone: lerTexto(dados, "telefone") || null,
      },
    }),
  );
  if (!resultado.ok) {
    return resultado.falha.status === 409 ? { ok: false, falha: { ...resultado.falha, sugestao: undefined } } : resultado;
  }
  revalidatePath("/administrativo/clientes");
  return resultado;
}

export async function atualizarCliente(id: string, dados: FormData): Promise<Resultado<ClienteDto>> {
  await exigirTela("clientes");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ClienteDto>(
    api.PUT("/api/administrativo/clientes/{id}", {
      params: { path: { id } },
      body: {
        nome: lerTexto(dados, "nome"),
        email: lerTexto(dados, "email"),
        telefone: lerTexto(dados, "telefone") || null,
      },
    }),
  );
  if (resultado.ok) {
    revalidatePath("/administrativo/clientes");
    revalidatePath(enderecoDoCliente(id));
  }
  return resultado;
}

export async function inativarCliente(id: string): Promise<Resultado<undefined>> {
  await exigirTela("clientes");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi(
    api.DELETE("/api/administrativo/clientes/{id}", { params: { path: { id } } }),
  );
  if (resultado.ok) {
    revalidatePath("/administrativo/clientes");
    revalidatePath(enderecoDoCliente(id));
  }
  return resultado;
}
