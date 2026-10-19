import type { ClienteDto } from "@/lib/api/dtos";
import { formatarDocumento, formatarTelefone, nomeDoTipoDeDocumento } from "@/lib/formatacao";

export function DadosDoCliente({ cliente }: { cliente: ClienteDto }) {
  const dados = [
    {
      rotulo: nomeDoTipoDeDocumento[cliente.tipoDocumento],
      valor: formatarDocumento(cliente.tipoDocumento, cliente.documento),
    },
    { rotulo: "E-mail", valor: cliente.email },
    { rotulo: "Telefone", valor: cliente.telefone ? formatarTelefone(cliente.telefone) : "Não informado" },
  ];

  return (
    <dl className="grid gap-3 sm:grid-cols-3">
      {dados.map(({ rotulo, valor }) => (
        <div key={rotulo} className="flex min-w-0 flex-col gap-1">
          <dt className="text-xs text-muted-foreground">{rotulo}</dt>
          <dd className="truncate text-sm font-medium" title={valor}>
            {valor}
          </dd>
        </div>
      ))}
    </dl>
  );
}
