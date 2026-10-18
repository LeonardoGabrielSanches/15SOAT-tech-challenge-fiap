import { CircleCheck } from "lucide-react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { PoliticaDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { nomeDoPapel, type Papel } from "@/lib/capacidades";

export async function ConexaoComApi({ papel }: { papel: Papel }) {
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<PoliticaDto>(
    api.GET("/api/administrativo/politicas/{nome}", { params: { path: { nome: papel } } }),
  );

  if (!resultado.ok) {
    return <AvisoDeFalha falha={resultado.falha} />;
  }

  return (
    <Alert>
      <CircleCheck />
      <AlertTitle>API conectada</AlertTitle>
      <AlertDescription>
        A API confirmou o acesso de {nomeDoPapel[papel]} (política {resultado.dados.politica}).
      </AlertDescription>
    </Alert>
  );
}
