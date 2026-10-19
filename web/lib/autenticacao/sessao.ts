import "server-only";
import { getToken } from "next-auth/jwt";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { auth } from "@/auth";
import { podeAcessarTela, telaInicial, type Papel, type TelaId } from "@/lib/capacidades";
import { configuracao } from "@/lib/configuracao";

export type Funcionario = {
  nome: string;
  papel: Papel | null;
};

export function enderecoDeEntrada(destino?: string): string {
  return destino ? `/entrar?callbackUrl=${encodeURIComponent(destino)}` : "/entrar";
}

export async function exigirSessao(): Promise<Funcionario> {
  const sessao = await auth();
  if (!sessao || sessao.erro) {
    redirect(enderecoDeEntrada());
  }
  return { nome: sessao.user?.name ?? "Funcionário", papel: sessao.papel };
}

export async function exigirTela(id: TelaId): Promise<Funcionario & { papel: Papel }> {
  const funcionario = await exigirSessao();
  if (!funcionario.papel) {
    redirect("/administrativo");
  }
  if (!podeAcessarTela(funcionario.papel, id)) {
    redirect(telaInicial(funcionario.papel).href);
  }
  return { ...funcionario, papel: funcionario.papel };
}

export async function exigirPapel(): Promise<Funcionario & { papel: Papel }> {
  const funcionario = await exigirSessao();
  if (!funcionario.papel) {
    redirect("/administrativo");
  }
  return { ...funcionario, papel: funcionario.papel };
}

export async function obterAccessToken(): Promise<string> {
  const token = await getToken({
    req: { headers: { cookie: (await cookies()).toString() } },
    secret: configuracao.authSecret,
    secureCookie: configuracao.cookieSeguro,
  });
  if (!token?.accessToken || token.erro) {
    redirect(enderecoDeEntrada());
  }
  return token.accessToken;
}
