import { Menu } from "@/components/menu";
import { Topo } from "@/components/topo";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { exigirSessao } from "@/lib/autenticacao/sessao";
import { telasDoMenu } from "@/lib/capacidades";

export default async function LayoutAdministrativo({ children }: LayoutProps<"/administrativo">) {
  const funcionario = await exigirSessao();

  return (
    <div className="flex min-h-screen flex-col">
      <Topo funcionario={funcionario} />
      {funcionario.papel ? (
        <div className="flex flex-1 flex-col md:flex-row">
          <aside className="border-b bg-background p-2 md:w-60 md:border-r md:border-b-0">
            <Menu telas={telasDoMenu(funcionario.papel)} />
          </aside>
          <main className="flex-1 p-4 md:p-6">{children}</main>
        </div>
      ) : (
        <main className="p-4 md:p-6">
          <Alert>
            <AlertTitle>Sem papel na oficina</AlertTitle>
            <AlertDescription>
              Seu usuário não tem o papel de Atendente, Mecânico ou Gerente. Peça ao Gerente para ajustar seu acesso no
              Keycloak.
            </AlertDescription>
          </Alert>
        </main>
      )}
    </div>
  );
}
