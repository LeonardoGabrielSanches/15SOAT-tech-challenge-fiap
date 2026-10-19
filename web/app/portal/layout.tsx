import { Wrench } from "lucide-react";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Portal do Cliente | Oficina",
};

export default function LayoutDoPortal({ children }: LayoutProps<"/portal">) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex h-14 items-center gap-2 border-b bg-background px-4">
        <Wrench className="size-5" />
        <span className="font-semibold">Oficina</span>
        <span className="text-sm text-muted-foreground">Portal do Cliente</span>
      </header>
      <main className="mx-auto w-full max-w-3xl flex-1 p-4 md:p-6">{children}</main>
    </div>
  );
}
