import type { NextRequest } from "next/server";
import { signOut } from "@/auth";

export async function POST(request: NextRequest) {
  if (request.headers.get("sec-fetch-site") === "cross-site") {
    return new Response(null, { status: 403 });
  }
  await signOut({ redirect: false });
  return new Response(null, { status: 303, headers: { Location: "/" } });
}
