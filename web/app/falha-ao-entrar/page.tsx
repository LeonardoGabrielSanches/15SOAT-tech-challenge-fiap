import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

export default function FalhaAoEntrar() {
  return (
    <main className="flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Não foi possível entrar</CardTitle>
          <CardDescription>O login no Keycloak não foi concluído.</CardDescription>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          Verifique se o Keycloak está no ar e tente novamente.
        </CardContent>
        <CardFooter>
          <Button asChild>
            <Link href="/entrar" prefetch={false}>
              Entrar novamente
            </Link>
          </Button>
        </CardFooter>
      </Card>
    </main>
  );
}
