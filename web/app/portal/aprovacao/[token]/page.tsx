import type { Metadata } from "next";
import Link from "next/link";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { decidirOrcamentoPeloToken } from "../acoes";
import { DecisaoDoOrcamento } from "./decisao-do-orcamento";

export const metadata: Metadata = {
  title: "Aprovação do orçamento | Oficina",
  robots: { index: false },
  referrer: "no-referrer",
};

export default async function DecidirOrcamento({ params }: PageProps<"/portal/aprovacao/[token]">) {
  const { token } = await params;
  return (
    <Card>
      <CardHeader>
        <CardTitle>Aprovação do orçamento</CardTitle>
        <CardDescription>
          Decida sobre o orçamento que a oficina enviou para você. Ao aprovar, a oficina começa o trabalho. Ao
          recusar, ela refaz o diagnóstico e envia uma nova versão do orçamento.
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <p className="text-sm text-muted-foreground">
          Para ver os itens e o total antes de decidir,{" "}
          <Link href="/portal/consulta" className="text-primary underline-offset-4 hover:underline">
            consulte a sua OS
          </Link>{" "}
          pelo Número da OS e o seu CPF/CNPJ.
        </p>
        <DecisaoDoOrcamento decidir={decidirOrcamentoPeloToken.bind(null, token)} />
      </CardContent>
    </Card>
  );
}
