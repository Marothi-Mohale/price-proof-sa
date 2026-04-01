"use client";

import { LogoMark } from "@/components/logo-mark";
import { ButtonLink } from "@/components/ui/button-link";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { useSession } from "@/components/providers/session-provider";

const featureItems = [
  {
    title: "Capture proof before you pay",
    description: "Record a shelf price, quote, image, PDF, or media note before the payment happens."
  },
  {
    title: "Compare it to the final charge",
    description: "Attach payment details, run discrepancy analysis, and keep the reasoning visible."
  },
  {
    title: "Generate a neutral complaint pack",
    description: "Prepare a professional PDF and JSON summary that a bank or merchant can review."
  }
];

export function LandingScreen() {
  const { session } = useSession();

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(251,191,36,0.18),transparent_24%),radial-gradient(circle_at_bottom_right,rgba(20,184,166,0.12),transparent_28%),linear-gradient(180deg,#fffdf8_0%,#f3ede1_100%)] px-4 py-6 sm:px-6 lg:px-8">
      <div className="mx-auto flex max-w-7xl flex-col gap-6">
        <header className="flex flex-col gap-4 rounded-[32px] border border-white/70 bg-white/85 p-6 shadow-[0_18px_45px_rgba(15,23,42,0.08)] backdrop-blur sm:flex-row sm:items-center sm:justify-between">
          <LogoMark />
          <div className="flex flex-wrap gap-3">
            <ButtonLink href={session ? "/dashboard" : "/auth"}>{session ? "Open dashboard" : "Sign in"}</ButtonLink>
            <ButtonLink href="/cases/new" variant="secondary">Start a new case</ButtonLink>
          </div>
        </header>

        <section className="grid gap-6 lg:grid-cols-[1.15fr_0.85fr]">
          <div className="rounded-[36px] border border-white/70 bg-[linear-gradient(135deg,rgba(15,23,42,0.98),rgba(22,42,65,0.94))] p-6 text-white shadow-[0_24px_60px_rgba(15,23,42,0.22)] sm:p-8">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-amber-300">Price discrepancy reporting</p>
            <h1 className="mt-4 font-display text-4xl font-bold tracking-tight sm:text-5xl">
              Build a clear pricing evidence trail from quote to complaint pack.
            </h1>
            <p className="mt-4 max-w-2xl text-base leading-7 text-slate-200">
              PriceProof SA helps shoppers document the displayed price, record the charged amount, and produce a
              factual complaint pack when the totals do not line up.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <ButtonLink href={session ? "/cases/new" : "/auth"} className="bg-amber-500 text-slate-950 hover:bg-amber-400">
                {session ? "Capture a new discrepancy" : "Create an account"}
              </ButtonLink>
              <ButtonLink href="/dashboard" variant="secondary" className="border-white/20 bg-white/10 text-white hover:bg-white/15">
                View case workflow
              </ButtonLink>
            </div>
          </div>

          <Card className="space-y-5">
            <div className="space-y-2">
              <CardTitle className="text-2xl">A mobile-first investigation workflow</CardTitle>
              <CardDescription>
                Start in store, continue on your phone, and finish with a bank-ready PDF once the evidence is complete.
              </CardDescription>
            </div>
            <div className="grid gap-4">
              {featureItems.map((item, index) => (
                <div key={item.title} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-amber-700">Step 0{index + 1}</p>
                  <p className="mt-2 text-base font-semibold text-slate-950">{item.title}</p>
                  <p className="mt-1 text-sm leading-6 text-slate-600">{item.description}</p>
                </div>
              ))}
            </div>
          </Card>
        </section>

        <section className="grid gap-6 md:grid-cols-3">
          <Card className="space-y-3">
            <CardTitle>Real backend integration</CardTitle>
            <CardDescription>
              The frontend talks directly to the ASP.NET Core API for cases, evidence uploads, OCR, analysis, and
              complaint pack generation.
            </CardDescription>
          </Card>
          <Card className="space-y-3">
            <CardTitle>Evidence-aware analysis</CardTitle>
            <CardDescription>
              Classifications stay conservative. If the evidence is weak or incomplete, the app says so clearly.
            </CardDescription>
          </Card>
          <Card className="space-y-3">
            <CardTitle>Professional output</CardTitle>
            <CardDescription>
              Every generated pack includes the timeline, amounts, explanation, declaration, and evidence inventory.
            </CardDescription>
          </Card>
        </section>
      </div>
    </main>
  );
}
