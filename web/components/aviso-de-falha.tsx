import { CircleAlert } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import type { Falha } from "@/lib/api/resultado";

export function AvisoDeFalha({ falha }: { falha: Falha }) {
  return (
    <Alert variant="destructive">
      <CircleAlert />
      <AlertTitle>{falha.titulo}</AlertTitle>
      <AlertDescription>
        <p>{falha.mensagem}</p>
        {falha.sugestao && <p>{falha.sugestao}</p>}
      </AlertDescription>
    </Alert>
  );
}
