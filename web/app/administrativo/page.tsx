import { redirect } from "next/navigation";
import { exigirSessao } from "@/lib/autenticacao/sessao";
import { telaInicial } from "@/lib/capacidades";

export default async function InicioAdministrativo() {
  const { papel } = await exigirSessao();
  if (papel) {
    redirect(telaInicial(papel).href);
  }
  return null;
}
