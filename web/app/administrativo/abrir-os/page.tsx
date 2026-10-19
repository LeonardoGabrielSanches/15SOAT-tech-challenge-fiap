import { Card, CardContent } from "@/components/ui/card";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { tela } from "@/lib/capacidades";
import { FormularioAbrirOs } from "./formulario-abrir-os";

export default async function AbrirOs() {
  await exigirTela("abrirOs");

  return (
    <div className="flex max-w-2xl flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold">{tela("abrirOs").titulo}</h1>
        <p className="text-muted-foreground">
          Cole o link ou o identificador da OS que o Atendente passou para fazer o diagnóstico.
        </p>
      </div>
      <Card>
        <CardContent>
          <FormularioAbrirOs />
        </CardContent>
      </Card>
    </div>
  );
}
