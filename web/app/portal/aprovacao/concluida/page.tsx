import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { confirmacoes, ehDecisaoConfirmada } from "../decisao-do-cliente";

export const metadata: Metadata = {
  title: "Orçamento decidido | Oficina",
  robots: { index: false },
};

export default async function DecisaoConcluida({ searchParams }: PageProps<"/portal/aprovacao/concluida">) {
  const { decisao } = await searchParams;
  if (!ehDecisaoConfirmada(decisao)) {
    redirect("/portal/aprovacao");
  }
  const { Icone, titulo, descricao } = confirmacoes[decisao];
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Icone className="size-5" />
          {titulo}
        </CardTitle>
        <CardDescription>{descricao}</CardDescription>
      </CardHeader>
      <CardFooter>
        <Button asChild variant="outline" size="lg" className="w-full sm:w-auto">
          <Link href="/portal/consulta">Acompanhar a OS</Link>
        </Button>
      </CardFooter>
    </Card>
  );
}
