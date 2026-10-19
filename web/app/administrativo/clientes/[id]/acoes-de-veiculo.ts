"use server";

import { revalidatePath } from "next/cache";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto, VeiculoDto } from "@/lib/api/dtos";
import { resultadoDaApi, type Falha, type Resultado } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";

function texto(dados: FormData, campo: string): string {
  const valor = dados.get(campo);
  return typeof valor === "string" ? valor.trim() : "";
}

function lerAno(dados: FormData): number {
  const ano = Number(texto(dados, "anoFabricacao"));
  return Number.isInteger(ano) ? ano : 0;
}

function enderecoDoCliente(id: string): string {
  return `/administrativo/clientes/${id}`;
}

function falhaLocal(status: number, titulo: string, mensagem: string): { ok: false; falha: Falha } {
  return { ok: false, falha: { status, titulo, mensagem } };
}

function semSugestaoParaPlacaRepetida<T>(resultado: Resultado<T>): Resultado<T> {
  if (!resultado.ok && resultado.falha.status === 409 && resultado.falha.mensagem.toLowerCase().includes("placa")) {
    return { ok: false, falha: { ...resultado.falha, sugestao: undefined } };
  }
  return resultado;
}

export async function cadastrarVeiculo(clienteId: string, dados: FormData): Promise<Resultado<VeiculoDto>> {
  await exigirTela("clientes");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<VeiculoDto>(
    api.POST("/api/administrativo/veiculos", {
      body: {
        placa: texto(dados, "placa"),
        marca: texto(dados, "marca"),
        modelo: texto(dados, "modelo"),
        anoFabricacao: lerAno(dados),
        proprietarioId: clienteId,
      },
    }),
  );
  if (resultado.ok) {
    revalidatePath(enderecoDoCliente(clienteId));
  }
  return semSugestaoParaPlacaRepetida(resultado);
}

export async function editarVeiculo(
  clienteId: string,
  veiculoId: string,
  dados: FormData,
): Promise<Resultado<VeiculoDto>> {
  await exigirTela("clientes");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<VeiculoDto>(
    api.PUT("/api/administrativo/veiculos/{id}", {
      params: { path: { id: veiculoId } },
      body: { marca: texto(dados, "marca"), modelo: texto(dados, "modelo"), anoFabricacao: lerAno(dados) },
    }),
  );
  if (resultado.ok) {
    revalidatePath(enderecoDoCliente(clienteId));
  }
  return resultado;
}

export async function inativarVeiculo(clienteId: string, veiculoId: string): Promise<Resultado<undefined>> {
  await exigirTela("clientes");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi(
    api.DELETE("/api/administrativo/veiculos/{id}", { params: { path: { id: veiculoId } } }),
  );
  if (resultado.ok) {
    revalidatePath(enderecoDoCliente(clienteId));
  }
  return resultado;
}

export async function buscarNovoProprietario(
  proprietarioAtualId: string,
  dados: FormData,
): Promise<Resultado<ClienteDto>> {
  await exigirTela("clientes");
  const documento = texto(dados, "documento");
  if (!documento) {
    return falhaLocal(422, "Dados inválidos", "Informe o CPF ou CNPJ do novo Proprietário atual.");
  }
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ClienteDto[]>(
    api.GET("/api/administrativo/clientes", { params: { query: { documento } } }),
  );
  if (!resultado.ok) {
    return resultado;
  }
  const [cliente] = resultado.dados;
  if (!cliente) {
    return falhaLocal(
      404,
      "Cliente não encontrado",
      "Nenhum Cliente cadastrado com este documento. Cadastre o novo Proprietário atual no Balcão e tente de novo.",
    );
  }
  if (cliente.id === proprietarioAtualId) {
    return falhaLocal(422, "Mesmo Proprietário atual", "Este Cliente já é o Proprietário atual do Veículo.");
  }
  if (!cliente.ativo) {
    return falhaLocal(409, "Cliente Inativo", `${cliente.nome} está Inativo e não pode receber o Veículo.`);
  }
  return { ok: true, dados: cliente };
}

export async function transferirVeiculo(
  veiculoId: string,
  proprietarioAtualId: string,
  novoProprietarioId: string,
): Promise<Resultado<VeiculoDto>> {
  await exigirTela("clientes");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<VeiculoDto>(
    api.POST("/api/administrativo/veiculos/{id}/transferencia", {
      params: { path: { id: veiculoId } },
      body: { novoProprietarioId },
    }),
  );
  if (resultado.ok) {
    revalidatePath(enderecoDoCliente(proprietarioAtualId));
    revalidatePath(enderecoDoCliente(novoProprietarioId));
  }
  return resultado;
}
