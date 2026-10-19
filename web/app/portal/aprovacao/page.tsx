import type { Metadata } from "next";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { FormularioDoToken } from "./formulario-do-token";

export const metadata: Metadata = {
  title: "Aprovação do orçamento | Oficina",
  robots: { index: false },
};

export default function InformarToken() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Aprovação do orçamento</CardTitle>
        <CardDescription>
          Cole o Token de aprovação que você recebeu junto com o orçamento para aprovar ou recusar a versão enviada
          pela oficina.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <FormularioDoToken />
      </CardContent>
    </Card>
  );
}
