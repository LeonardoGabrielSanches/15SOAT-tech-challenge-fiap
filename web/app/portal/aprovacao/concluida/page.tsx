import { CircleCheck, CircleX } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

export const metadata: Metadata = {
  title: "Orçamento decidido | Oficina",
  robots: { index: false },
};

const confirmacoes = {
  aprovado: {
    Icone: CircleCheck,
    titulo: "Orçamento aprovado",
    descricao: "Obrigado! A oficina já pode começar o trabalho no seu Veículo.",
  },
  recusado: {
    Icone: CircleX,
    titulo: "Orçamento recusado",
    descricao: "A oficina vai refazer o diagnóstico e enviar uma nova versão do orçamento, com um novo Token de aprovação.",
  },
};

function ehDecisao(valor: unknown): valor is keyof typeof confirmacoes {
  return valor === "aprovado" || valor === "recusado";
}

export default async function DecisaoConcluida({ searchParams }: PageProps<"/portal/aprovacao/concluida">) {
  const { decisao } = await searchParams;
  if (!ehDecisao(decisao)) {
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
