"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { LogoMark } from "@/components/logo-mark";
import { useSession } from "@/components/providers/session-provider";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { FieldShell, TextInput } from "@/components/ui/field";
import { flattenZodErrors, signInSchema, signUpSchema } from "@/lib/validators";

export function AuthScreen({ nextDestination }: { nextDestination?: string }) {
  const router = useRouter();
  const { authBusy, authError, currentUser, session, signIn, signUp } = useSession();
  const [mode, setMode] = useState<"signIn" | "signUp">("signIn");
  const [signInValues, setSignInValues] = useState({ email: "", password: "" });
  const [signUpValues, setSignUpValues] = useState({ displayName: "", email: "", password: "", confirmPassword: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (session && currentUser) {
      router.replace(nextDestination ?? "/dashboard");
    }
  }, [currentUser, nextDestination, router, session]);

  async function handleSignIn(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = signInSchema.safeParse(signInValues);

    if (!parsed.success) {
      setErrors(flattenZodErrors(parsed.error.issues));
      return;
    }

    setErrors({});

    try {
      await signIn(parsed.data);
      router.replace(nextDestination ?? "/dashboard");
    } catch {
    }
  }

  async function handleSignUp(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = signUpSchema.safeParse(signUpValues);

    if (!parsed.success) {
      setErrors(flattenZodErrors(parsed.error.issues));
      return;
    }

    setErrors({});

    try {
      await signUp({
        displayName: parsed.data.displayName,
        email: parsed.data.email,
        password: parsed.data.password
      });
      router.replace(nextDestination ?? "/dashboard");
    } catch {
    }
  }

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(251,191,36,0.18),transparent_24%),radial-gradient(circle_at_bottom_right,rgba(20,184,166,0.12),transparent_28%),linear-gradient(180deg,#fffdf8_0%,#f3ede1_100%)] px-4 py-6 sm:px-6 lg:px-8">
      <div className="mx-auto grid max-w-6xl gap-6 lg:grid-cols-[0.95fr_1.05fr]">
        <Card className="space-y-5 bg-[linear-gradient(145deg,rgba(15,23,42,0.98),rgba(22,42,65,0.94))] text-white">
          <LogoMark />
          <div className="space-y-3">
            <CardTitle className="text-3xl text-white">Sign in to your evidence workspace</CardTitle>
            <CardDescription className="text-slate-200">
              This build uses the live API session endpoints. Sign up creates a real user record with a hashed
              password, and sign in restores your workspace with a secure session cookie.
            </CardDescription>
          </div>
          <div className="rounded-[28px] border border-white/10 bg-white/5 p-5 text-sm leading-6 text-slate-200">
            Passwords must be at least 12 characters long and include uppercase, lowercase, a number, and a symbol.
          </div>
        </Card>

        <Card className="space-y-5">
          <div className="flex gap-2 rounded-2xl bg-slate-100 p-2">
            <button
              type="button"
              onClick={() => {
                setMode("signIn");
                setErrors({});
              }}
              className={`flex-1 rounded-2xl px-4 py-3 text-sm font-semibold transition ${
                mode === "signIn" ? "bg-white text-slate-950 shadow-sm" : "text-slate-600"
              }`}
            >
              Sign in
            </button>
            <button
              type="button"
              onClick={() => {
                setMode("signUp");
                setErrors({});
              }}
              className={`flex-1 rounded-2xl px-4 py-3 text-sm font-semibold transition ${
                mode === "signUp" ? "bg-white text-slate-950 shadow-sm" : "text-slate-600"
              }`}
            >
              Sign up
            </button>
          </div>

          {authError ? (
            <div className="rounded-3xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{authError}</div>
          ) : null}

          {mode === "signIn" ? (
            <form className="space-y-4" onSubmit={handleSignIn}>
              <FieldShell htmlFor="sign-in-email" label="Email address" error={errors.email} required>
                <TextInput
                  id="sign-in-email"
                  type="email"
                  placeholder="name@example.com"
                  autoComplete="email"
                  value={signInValues.email}
                  onChange={(event) => setSignInValues((current) => ({ ...current, email: event.target.value }))}
                />
              </FieldShell>
              <FieldShell htmlFor="sign-in-password" label="Password" error={errors.password} required>
                <TextInput
                  id="sign-in-password"
                  type="password"
                  placeholder="Enter your password"
                  autoComplete="current-password"
                  value={signInValues.password}
                  onChange={(event) => setSignInValues((current) => ({ ...current, password: event.target.value }))}
                />
              </FieldShell>
              <Button type="submit" busy={authBusy} className="w-full">
                Continue to dashboard
              </Button>
            </form>
          ) : (
            <form className="space-y-4" onSubmit={handleSignUp}>
              <FieldShell htmlFor="sign-up-name" label="Display name" error={errors.displayName} required>
                <TextInput
                  id="sign-up-name"
                  type="text"
                  placeholder="Your name"
                  autoComplete="name"
                  value={signUpValues.displayName}
                  onChange={(event) => setSignUpValues((current) => ({ ...current, displayName: event.target.value }))}
                />
              </FieldShell>
              <FieldShell htmlFor="sign-up-email" label="Email address" error={errors.email} required>
                <TextInput
                  id="sign-up-email"
                  type="email"
                  placeholder="name@example.com"
                  autoComplete="email"
                  value={signUpValues.email}
                  onChange={(event) => setSignUpValues((current) => ({ ...current, email: event.target.value }))}
                />
              </FieldShell>
              <FieldShell htmlFor="sign-up-password" label="Password" error={errors.password} required>
                <TextInput
                  id="sign-up-password"
                  type="password"
                  placeholder="Create a strong password"
                  autoComplete="new-password"
                  value={signUpValues.password}
                  onChange={(event) => setSignUpValues((current) => ({ ...current, password: event.target.value }))}
                />
              </FieldShell>
              <FieldShell htmlFor="sign-up-confirm-password" label="Confirm password" error={errors.confirmPassword} required>
                <TextInput
                  id="sign-up-confirm-password"
                  type="password"
                  placeholder="Confirm your password"
                  autoComplete="new-password"
                  value={signUpValues.confirmPassword}
                  onChange={(event) => setSignUpValues((current) => ({ ...current, confirmPassword: event.target.value }))}
                />
              </FieldShell>
              <Button type="submit" busy={authBusy} className="w-full">
                Create account
              </Button>
            </form>
          )}
        </Card>
      </div>
    </main>
  );
}
