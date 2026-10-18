import type { DefaultSession } from "next-auth";
import type { Papel } from "@/lib/capacidades";

declare module "next-auth" {
  interface Session {
    user: DefaultSession["user"];
    papel: Papel | null;
    erro?: "RefreshTokenError";
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    nome?: string;
    accessToken?: string;
    refreshToken?: string;
    expiraEm?: number;
    papel?: Papel | null;
    erro?: "RefreshTokenError";
  }
}
