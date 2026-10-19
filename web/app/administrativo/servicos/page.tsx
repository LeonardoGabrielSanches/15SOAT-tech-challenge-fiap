import { AtivoOuInativo } from "@/components/ativo-ou-inativo";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { InativarComConfirmacao } from "@/components/inativar-com-confirmacao";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { apiAdministrativa } from "@/lib/api/cliente";
import type { ServicoDto } from "@/lib/api/dtos";
import { resultadoDaApi } from "@/lib/api/resultado";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { formatarDinheiro } from "@/lib/formatacao";
import { cn } from "@/lib/utils";
import { FormularioDeServico } from "./formulario-de-servico";
import { inativarServico } from "./acoes";

export default async function Servicos() {
  await exigirTela("servicos");
  const api = await apiAdministrativa();
  const resultado = await resultadoDaApi<ServicoDto[]>(api.GET("/api/administrativo/servicos"));

  return (
    <div className="flex max-w-5xl flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="text-2xl font-semibold">Serviços</h1>
          <p className="text-muted-foreground">Catálogo de Serviços da oficina. Os Inativos aparecem destacados.</p>
        </div>
        <FormularioDeServico />
      </div>
      {!resultado.ok ? (
        <AvisoDeFalha falha={resultado.falha} />
      ) : resultado.dados.length === 0 ? (
        <p className="text-muted-foreground">Nenhum Serviço cadastrado.</p>
      ) : (
        <TabelaDeServicos servicos={resultado.dados} />
      )}
    </div>
  );
}

function TabelaDeServicos({ servicos }: { servicos: ServicoDto[] }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Nome</TableHead>
          <TableHead>Descrição</TableHead>
          <TableHead className="text-right">Preço</TableHead>
          <TableHead>Situação</TableHead>
          <TableHead className="text-right">Ações</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {servicos.map((servico) => (
          <TableRow key={servico.id} className={cn(!servico.ativo && "bg-muted/60 text-muted-foreground")}>
            <TableCell className="font-medium">{servico.nome}</TableCell>
            <TableCell className="max-w-sm whitespace-normal">{servico.descricao ?? "—"}</TableCell>
            <TableCell className="text-right tabular-nums">{formatarDinheiro(servico.preco)}</TableCell>
            <TableCell>
              <AtivoOuInativo ativo={servico.ativo} />
            </TableCell>
            <TableCell>
              <div className="flex justify-end gap-2">
                <FormularioDeServico servico={servico} />
                {servico.ativo && (
                  <InativarComConfirmacao
                    titulo={`Inativar o Serviço ${servico.nome}?`}
                    descricao="O Serviço deixa de ser oferecido em novas OS e continua nas OS em que já foi usado."
                    acao={inativarServico.bind(null, servico.id)}
                  />
                )}
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
