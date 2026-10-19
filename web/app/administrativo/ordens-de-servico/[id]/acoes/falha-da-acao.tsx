"use client";

import { RefreshCw } from "lucide-react";
import { useRouter } from "next/navigation";
import { useTransition } from "react";
import { AvisoDeFalha } from "@/components/aviso-de-falha";
import { Button } from "@/components/ui/button";
import type { Falha } from "@/lib/api/resultado";

export function FalhaDaAcao({ falha }: { falha: Falha }) {
  const router = useRouter();
  const [recarregando, startTransition] = useTransition();

  return (
    <div className="flex flex-col gap-2">
      <AvisoDeFalha falha={falha} />
      {falha.status === 409 && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="self-start"
          disabled={recarregando}
          onClick={() => startTransition(() => router.refresh())}
        >
          <RefreshCw />
          {recarregando ? "Recarregando..." : "Recarregar OS"}
        </Button>
      )}
    </div>
  );
}
