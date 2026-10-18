import NextAuth, { customFetch } from "next-auth";
import type { JWT } from "next-auth/jwt";
import Keycloak from "next-auth/providers/keycloak";
import { papelPrincipal } from "@/lib/capacidades";
import { configuracao } from "@/lib/configuracao";

const margemDeRenovacaoEmSegundos = 60;

type RespostaDeToken = {
  access_token: string;
  expires_in: number;
  refresh_token?: string;
};

function papeisDoAccessToken(accessToken: string): string[] {
  const [, carga] = accessToken.split(".");
  const conteudo = JSON.parse(Buffer.from(carga, "base64url").toString("utf8"));
  return conteudo?.realm_access?.roles ?? [];
}

function paraUrlInterna(url: string): string {
  const { keycloakUrlPublica, keycloakUrlInterna } = configuracao;
  return url.startsWith(keycloakUrlPublica) ? keycloakUrlInterna + url.slice(keycloakUrlPublica.length) : url;
}

const fetchPelaRedeInterna: typeof fetch = (entrada, init) => {
  if (entrada instanceof Request) {
    return fetch(new Request(paraUrlInterna(entrada.url), entrada), init);
  }
  return fetch(paraUrlInterna(entrada.toString()), init);
};

async function chamarKeycloak(caminho: string, parametros: Record<string, string>): Promise<Response> {
  return fetch(`${configuracao.keycloakUrlInterna}/protocol/openid-connect/${caminho}`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      client_id: configuracao.keycloakClientId,
      client_secret: configuracao.keycloakClientSecret,
      ...parametros,
    }),
    cache: "no-store",
  });
}

function comTokens(token: JWT, tokens: RespostaDeToken): JWT {
  return {
    ...token,
    accessToken: tokens.access_token,
    refreshToken: tokens.refresh_token ?? token.refreshToken,
    expiraEm: Math.floor(Date.now() / 1000) + tokens.expires_in,
    papel: papelPrincipal(papeisDoAccessToken(tokens.access_token)),
    erro: undefined,
  };
}

async function renovar(token: JWT): Promise<JWT> {
  if (!token.refreshToken) {
    return { ...token, erro: "RefreshTokenError" };
  }
  try {
    const resposta = await chamarKeycloak("token", { grant_type: "refresh_token", refresh_token: token.refreshToken });
    if (!resposta.ok) {
      return { ...token, erro: "RefreshTokenError" };
    }
    return comTokens(token, (await resposta.json()) as RespostaDeToken);
  } catch {
    return { ...token, erro: "RefreshTokenError" };
  }
}

export const { handlers, auth, signIn, signOut } = NextAuth(() => ({
  trustHost: true,
  session: { strategy: "jwt" },
  pages: { signIn: "/entrar", error: "/falha-ao-entrar" },
  providers: [
    Keycloak({
      clientId: configuracao.keycloakClientId,
      clientSecret: configuracao.keycloakClientSecret,
      issuer: configuracao.keycloakUrlPublica,
      checks: ["pkce", "state"],
      [customFetch]: fetchPelaRedeInterna,
    }),
  ],
  callbacks: {
    authorized({ auth: sessao }) {
      return Boolean(sessao) && !sessao?.erro;
    },
    async jwt({ token, account, profile }) {
      if (account?.access_token) {
        return comTokens(
          { ...token, nome: profile?.name ?? profile?.preferred_username ?? token.name ?? undefined },
          {
            access_token: account.access_token,
            expires_in: (account.expires_at ?? 0) - Math.floor(Date.now() / 1000),
            refresh_token: account.refresh_token,
          },
        );
      }
      if (token.expiraEm && Date.now() < (token.expiraEm - margemDeRenovacaoEmSegundos) * 1000) {
        return token;
      }
      return renovar(token);
    },
    session({ session, token }) {
      session.user.name = token.nome ?? session.user.name;
      session.papel = token.papel ?? null;
      session.erro = token.erro;
      return session;
    },
  },
  events: {
    async signOut(mensagem) {
      const token = "token" in mensagem ? mensagem.token : null;
      if (token?.refreshToken) {
        await chamarKeycloak("logout", { refresh_token: token.refreshToken }).catch(() => undefined);
      }
    },
  },
}));
