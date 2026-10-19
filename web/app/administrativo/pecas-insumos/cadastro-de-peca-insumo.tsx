"use client";

import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAcaoDeFormulario } from "@/lib/acao-de-formulario";
import { nomeDoTipoPecaInsumo } from "@/lib/formatacao";
import { cadastrarPecaInsumo } from "./acoes";

export function CadastroDePecaInsumo() {
  const { resultado, enviar, pendente } = useAcaoDeFormulario(cadastrarPecaInsumo, (_, formulario) =>
    formulario.reset(),
  );

  return (
    <Card>
      <CardHeader>
        <CardTitle>Cadastrar Peça/insumo</CardTitle>
        <CardDescription>
          O tipo não pode ser alterado depois. Peça é contada em unidades inteiras; Insumo aceita frações.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={enviar} className="flex flex-col gap-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="cadastro-nome">Nome</Label>
              <Input id="cadastro-nome" name="nome" required maxLength={200} />
            </div>
            <fieldset className="flex flex-col gap-2">
              <legend className="mb-2 text-sm font-medium">Tipo</legend>
              <div className="flex h-8 items-center gap-4">
                {(["Peca", "Insumo"] as const).map((tipo) => (
                  <label key={tipo} className="flex items-center gap-2 text-sm">
                    <input type="radio" name="tipo" value={tipo} defaultChecked={tipo === "Peca"} required />
                    {nomeDoTipoPecaInsumo[tipo]}
                  </label>
                ))}
              </div>
            </fieldset>
            <div className="flex flex-col gap-2">
              <Label htmlFor="cadastro-unidade">Unidade de medida</Label>
              <Input id="cadastro-unidade" name="unidadeDeMedida" required maxLength={30} placeholder="un, L, kg" />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="cadastro-preco">Preço (R$)</Label>
              <Input id="cadastro-preco" name="preco" required inputMode="decimal" placeholder="0,00" />
            </div>
          </div>
          {resultado && !resultado.ok && <AvisoDeFalha falha={resultado.falha} />}
          {resultado?.ok && (
            <p className="text-sm text-muted-foreground" role="status">
              {resultado.dados.nome} cadastrada com saldo zero.
            </p>
          )}
          <div>
            <Button type="submit" disabled={pendente}>
              {pendente ? "Cadastrando..." : "Cadastrar"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
