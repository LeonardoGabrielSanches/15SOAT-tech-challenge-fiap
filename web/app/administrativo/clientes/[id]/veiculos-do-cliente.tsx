import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { ClienteDto } from "@/lib/api/dtos";

export function VeiculosDoCliente({ cliente }: { cliente: ClienteDto }) {
  return (
    <Card id="veiculos">
      <CardHeader>
        <CardTitle>Veículos</CardTitle>
        <CardDescription>Veículos de {cliente.nome}.</CardDescription>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-muted-foreground">Esta seção ainda está em construção.</p>
      </CardContent>
    </Card>
  );
}
