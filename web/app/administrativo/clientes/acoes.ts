"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto } from "@/lib/api/dtos";
import { resultadoDaApi, type Falha } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { lerTexto } from "@/lib/dados-do-formulario";
import { enderecoDoCliente } from "./endereco-do-cliente";
import type { EstadoDoFormularioDeCliente, ValoresDoCliente } from "./formulario";

function valoresDoFormulario(dados: FormData): ValoresDoCliente {
  return {
    nome: lerTexto(dados, "nome"),
    documento: lerTexto(dados, "documento"),
    email: lerTexto(dados, "email"),
    telefone: lerTexto(dados, "telefone"),
  };
}

export async function cadastrarCliente(
  estado: EstadoDoFormularioDeCliente,
  dados: FormData,
): Promise<EstadoDoFormularioDeCliente> {
  await exigirTela("balcao");
  const valores = valoresDoFormulario(dados);
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ClienteDto>(
    api.POST("/api/administrativo/clientes", {
      body: {
        nome: valores.nome,
        documento: valores.documento,
        email: valores.email,
        telefone: valores.telefone || null,
      },
    }),
  );
  if (!resultado.ok) {
    const falha = resultado.falha.status === 409 ? { ...resultado.falha, sugestao: undefined } : resultado.falha;
    return { falha, valores, tentativa: estado.tentativa + 1 };
  }
  revalidatePath("/administrativo/clientes");
  redirect(enderecoDoCliente(resultado.dados.id));
}

export async function atualizarCliente(
  id: string,
  estado: EstadoDoFormularioDeCliente,
  dados: FormData,
): Promise<EstadoDoFormularioDeCliente> {
  await exigirTela("clientes");
  const valores = { ...valoresDoFormulario(dados), documento: estado.valores.documento };
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ClienteDto>(
    api.PUT("/api/administrativo/clientes/{id}", {
      params: { path: { id } },
      body: { nome: valores.nome, email: valores.email, telefone: valores.telefone || null },
    }),
  );
  if (!resultado.ok) {
    return { falha: resultado.falha, valores, tentativa: estado.tentativa + 1 };
  }
  revalidatePath("/administrativo/clientes");
  revalidatePath(enderecoDoCliente(id));
  redirect(enderecoDoCliente(id));
}

export async function inativarCliente(id: string): Promise<Falha | null> {
  await exigirTela("clientes");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi(
    api.DELETE("/api/administrativo/clientes/{id}", { params: { path: { id } } }),
  );
  if (!resultado.ok) {
    return resultado.falha;
  }
  revalidatePath("/administrativo/clientes");
  revalidatePath(enderecoDoCliente(id));
  return null;
}
