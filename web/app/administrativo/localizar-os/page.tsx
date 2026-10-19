import { Card, CardContent } from "@/components/ui/card";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { tela } from "@/lib/capacidades";
import { FormularioDeLocalizacao } from "./formulario-de-localizacao";

export default async function LocalizarOs() {
  await exigirTela("localizarOs");

  return (
    <div className="flex max-w-2xl flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold">{tela("localizarOs").titulo}</h1>
        <p className="text-muted-foreground">
          Cole o link ou o identificador da OS que o Atendente passou para fazer o diagnóstico.
        </p>
      </div>
      <Card>
        <CardContent>
          <FormularioDeLocalizacao />
        </CardContent>
      </Card>
    </div>
  );
}
