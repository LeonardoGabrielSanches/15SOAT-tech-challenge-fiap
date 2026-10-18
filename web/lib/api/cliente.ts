import "server-only";
import createClient from "openapi-fetch";
import { obterAccessToken } from "@/lib/autenticacao/sessao";
import { configuracao } from "@/lib/configuracao";
import type { paths } from "@/lib/api/openapi";

export async function apiAdministrativa() {
  const accessToken = await obterAccessToken();
  return createClient<paths>({
    baseUrl: configuracao.apiUrl,
    headers: { Authorization: `Bearer ${accessToken}` },
    cache: "no-store",
  });
}

export function apiPublica() {
  return createClient<paths>({ baseUrl: configuracao.apiUrl, cache: "no-store" });
}
