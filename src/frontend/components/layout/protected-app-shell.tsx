"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { LogoMark } from "@/components/logo-mark";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useSession } from "@/components/providers/session-provider";
import { cn } from "@/lib/utils";

const baseNavigationItems = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/cases/new", label: "New Case" },
  { href: "/settings", label: "Settings" }
];

export function ProtectedAppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { currentUser, initializing, session, signOut } = useSession();
  const isAdmin = currentUser?.isAdmin ?? session?.isAdmin ?? false;
  const navigationItems = isAdmin
    ? [...baseNavigationItems, { href: "/admin/risk", label: "Risk Desk" }]
    : baseNavigationItems;

  useEffect(() => {
    if (!initializing && !session) {
      const nextPath = pathname && pathname !== "/" ? pathname : "/dashboard";
      router.replace(`/auth?next=${encodeURIComponent(nextPath)}`);
    }
  }, [initializing, pathname, router, session]);

  if (initializing || !session) {
    return (
      <main className="flex min-h-screen items-center justify-center px-4 py-8">
        <Card className="w-full max-w-md space-y-4 text-center">
          <LogoMark className="justify-center" />
          <div className="space-y-2">
            <p className="font-display text-xl font-semibold text-slate-950">Loading your workspace</p>
            <p className="text-sm text-slate-600">
              We&apos;re restoring your session and checking the latest case data.
            </p>
          </div>
        </Card>
      </main>
    );
  }

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(251,191,36,0.18),transparent_28%),radial-gradient(circle_at_top_right,rgba(20,184,166,0.12),transparent_30%),linear-gradient(180deg,#fffdf7_0%,#f5efe3_100%)]">
      <div className="mx-auto flex max-w-7xl gap-6 px-4 py-4 pb-28 lg:px-8 lg:pb-10">
        <aside className="hidden w-80 shrink-0 lg:block">
          <div className="sticky top-4 space-y-4">
            <Card className="space-y-5">
              <LogoMark />
              <div className="space-y-2">
                <p className="font-display text-2xl font-semibold text-slate-950">Consumer evidence workspace</p>
                <p className="text-sm leading-6 text-slate-600">
                  Capture the quoted price, attach proof of payment, analyze the discrepancy, and prepare a neutral
                  complaint pack.
                </p>
              </div>
              <nav className="space-y-2">
                {navigationItems.map((item) => {
                  const isActive = pathname?.startsWith(item.href);

                  return (
                    <Link
                      key={item.href}
                      href={item.href}
                      className={cn(
                        "flex items-center justify-between rounded-2xl px-4 py-3 text-sm font-semibold transition",
                        isActive ? "bg-ink text-white" : "bg-slate-50 text-slate-700 hover:bg-slate-100"
                      )}
                    >
                      {item.label}
                    </Link>
                  );
                })}
              </nav>
            </Card>
            <Card className="space-y-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Signed in as</p>
                <p className="mt-2 text-sm font-semibold text-slate-950">{currentUser?.displayName ?? session.displayName}</p>
                <p className="text-sm text-slate-600">{currentUser?.email ?? session.email}</p>
                {isAdmin ? <p className="mt-2 text-xs font-semibold uppercase tracking-[0.2em] text-amber-700">Admin access</p> : null}
              </div>
              <Button
                variant="secondary"
                onClick={() => {
                  signOut();
                  router.replace("/auth");
                }}
              >
                Sign out
              </Button>
            </Card>
          </div>
        </aside>

        <main className="min-w-0 flex-1">{children}</main>
      </div>

      <nav className="fixed bottom-4 left-4 right-4 z-40 rounded-[28px] border border-white/70 bg-white/95 p-3 shadow-[0_18px_45px_rgba(15,23,42,0.18)] backdrop-blur lg:hidden">
        <div className="grid grid-cols-3 gap-2">
          {navigationItems.map((item) => {
            const isActive = pathname?.startsWith(item.href);

            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "rounded-2xl px-3 py-3 text-center text-sm font-semibold transition",
                  isActive ? "bg-ink text-white" : "text-slate-600 hover:bg-slate-100"
                )}
              >
                {item.label}
              </Link>
            );
          })}
        </div>
      </nav>
    </div>
  );
}
