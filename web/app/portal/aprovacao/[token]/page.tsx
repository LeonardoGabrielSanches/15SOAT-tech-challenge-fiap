import type { Metadata } from "next";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { aprovarOrcamento, recusarOrcamento } from "../acoes";
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
      <CardContent>
        <DecisaoDoOrcamento aprovar={aprovarOrcamento.bind(null, token)} recusar={recusarOrcamento.bind(null, token)} />
      </CardContent>
    </Card>
  );
}
