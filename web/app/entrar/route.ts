import type { NextRequest } from "next/server";
import { signIn } from "@/auth";

function destinoSeguro(callbackUrl: string | null, origem: string): string {
  if (!callbackUrl) {
    return "/administrativo";
  }
  try {
    const url = new URL(callbackUrl, origem);
    return `${url.pathname}${url.search}`;
  } catch {
    return "/administrativo";
  }
}

export async function GET(request: NextRequest) {
  await signIn("keycloak", {
    redirectTo: destinoSeguro(request.nextUrl.searchParams.get("callbackUrl"), request.nextUrl.origin),
  });
}
