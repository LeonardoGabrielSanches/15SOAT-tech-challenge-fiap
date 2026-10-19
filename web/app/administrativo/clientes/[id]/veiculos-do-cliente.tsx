import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ClienteDto, VeiculoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { cn } from "@/lib/utils";
import { SituacaoDoCliente } from "../situacao-do-cliente";
import { AcoesDoVeiculo } from "./acoes-do-veiculo";
import { FormularioDeVeiculo } from "./formulario-de-veiculo";

export async function VeiculosDoCliente({ cliente }: { cliente: ClienteDto }) {
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<VeiculoDto[]>(
    api.GET("/api/administrativo/clientes/{clienteId}/veiculos", { params: { path: { clienteId: cliente.id } } }),
  );

  return (
    <Card id="veiculos">
      <CardHeader>
        <CardTitle>Veículos</CardTitle>
        <CardDescription>
          {cliente.ativo
            ? `Veículos de ${cliente.nome}. Os Inativos aparecem destacados.`
            : "Cliente Inativo: não é possível cadastrar Veículos."}
        </CardDescription>
        {cliente.ativo && (
          <CardAction>
            <FormularioDeVeiculo clienteId={cliente.id} />
          </CardAction>
        )}
      </CardHeader>
      <CardContent>
        {!resultado.ok ? (
          <AvisoDeFalha falha={resultado.falha} />
        ) : resultado.dados.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nenhum Veículo cadastrado para este Cliente.</p>
        ) : (
          <TabelaDeVeiculos cliente={cliente} veiculos={resultado.dados} />
        )}
      </CardContent>
    </Card>
  );
}

function TabelaDeVeiculos({ cliente, veiculos }: { cliente: ClienteDto; veiculos: VeiculoDto[] }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Placa</TableHead>
          <TableHead>Marca</TableHead>
          <TableHead>Modelo</TableHead>
          <TableHead className="text-right">Ano</TableHead>
          <TableHead>Situação</TableHead>
          <TableHead className="text-right">Ações</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {veiculos.map((veiculo) => (
          <TableRow key={veiculo.id} className={cn(!veiculo.ativo && "bg-muted/60 text-muted-foreground")}>
            <TableCell className="font-mono font-medium">{veiculo.placa}</TableCell>
            <TableCell>{veiculo.marca}</TableCell>
            <TableCell>{veiculo.modelo}</TableCell>
            <TableCell className="text-right tabular-nums">{veiculo.anoFabricacao}</TableCell>
            <TableCell>
              <SituacaoDoCliente ativo={veiculo.ativo} />
            </TableCell>
            <TableCell>
              <AcoesDoVeiculo cliente={cliente} veiculo={veiculo} />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
