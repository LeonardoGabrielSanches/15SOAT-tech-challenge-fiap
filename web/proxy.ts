import { NextResponse, type NextFetchEvent, type NextRequest } from "next/server";
import { auth } from "@/auth";

type Proxy = (request: NextRequest, evento: NextFetchEvent) => Promise<Response>;

const verificarSessao = auth as unknown as Proxy;

function seguirComCookiesRenovados(request: NextRequest, resposta: Response): Response {
  const cookiesRenovados = resposta.headers.getSetCookie();
  if (cookiesRenovados.length === 0) {
    return resposta;
  }
  for (const cookie of new NextResponse(null, { headers: resposta.headers }).cookies.getAll()) {
    if (!cookie.value || cookie.maxAge === 0) {
      request.cookies.delete(cookie.name);
    } else {
      request.cookies.set(cookie.name, cookie.value);
    }
  }
  const seguinte = NextResponse.next({ request: { headers: new Headers(request.headers) } });
  if (request.method === "GET") {
    for (const cookie of cookiesRenovados) {
      seguinte.headers.append("set-cookie", cookie);
    }
  }
  return seguinte;
}

export async function proxy(request: NextRequest, evento: NextFetchEvent) {
  const resposta = await verificarSessao(request, evento);
  return resposta.headers.has("location") ? resposta : seguirComCookiesRenovados(request, resposta);
}

export const config = {
  matcher: ["/administrativo/:path*"],
};
