"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { api } from "@/lib/api";
import { writeSession } from "@/lib/storage";

export function VerifyEmailScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const token = searchParams.get("token") ?? "";
  const [status, setStatus] = useState<"idle" | "working" | "success" | "error">("idle");
  const [message, setMessage] = useState("Confirming your email verification link.");

  useEffect(() => {
    let isCancelled = false;

    async function verify() {
      if (!email || !token) {
        setStatus("error");
        setMessage("This verification link is incomplete. Request a fresh verification email from the sign-in page.");
        return;
      }

      setStatus("working");

      try {
        const session = await api.confirmEmailVerification({ email, token });
        if (isCancelled) {
          return;
        }

        writeSession(session);
        setStatus("success");
        setMessage(session.message ?? "Email verified successfully. Your secure session is ready.");
      } catch (error) {
        if (isCancelled) {
          return;
        }

        setStatus("error");
        setMessage(error instanceof Error ? error.message : "We could not verify this email link.");
      }
    }

    void verify();
    return () => {
      isCancelled = true;
    };
  }, [email, token]);

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(251,191,36,0.18),transparent_24%),radial-gradient(circle_at_bottom_right,rgba(20,184,166,0.12),transparent_28%),linear-gradient(180deg,#fffdf8_0%,#f3ede1_100%)] px-4 py-6 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-2xl">
        <Card className="space-y-5">
          <CardTitle className="text-3xl">Verify email</CardTitle>
          <CardDescription>
            PriceProof SA uses verified email addresses for account recovery and public-facing complaint workflows.
          </CardDescription>
          <div className={`rounded-3xl px-4 py-4 text-sm leading-6 ${status === "error" ? "border border-rose-200 bg-rose-50 text-rose-700" : status === "success" ? "border border-emerald-200 bg-emerald-50 text-emerald-700" : "border border-slate-200 bg-slate-50 text-slate-700"}`}>
            {message}
          </div>
          <div className="flex flex-wrap gap-3">
            {status === "success" ? (
              <Button onClick={() => router.push("/dashboard")}>Continue to dashboard</Button>
            ) : null}
            <Button variant="secondary" onClick={() => router.push("/auth")}>
              Back to sign in
            </Button>
          </div>
        </Card>
      </div>
    </main>
  );
}
