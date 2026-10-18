import { ConexaoComApi } from "@/components/conexao-com-api";
import { exigirTela } from "@/lib/autenticacao/sessao";
import { tela, type TelaId } from "@/lib/capacidades";

export async function TelaEmConstrucao({ id }: { id: TelaId }) {
  const { papel } = await exigirTela(id);

  return (
    <div className="flex max-w-3xl flex-col gap-4">
      <h1 className="text-2xl font-semibold">{tela(id).titulo}</h1>
      <p className="text-muted-foreground">Esta tela ainda está em construção.</p>
      <ConexaoComApi papel={papel} />
    </div>
  );
}
