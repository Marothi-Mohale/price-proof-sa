"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { LogoMark } from "@/components/logo-mark";
import { useSession } from "@/components/providers/session-provider";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { FieldShell, TextInput } from "@/components/ui/field";
import { api } from "@/lib/api";
import { flattenZodErrors, signInSchema, signUpSchema } from "@/lib/validators";

export function AuthScreen({ nextDestination }: { nextDestination?: string }) {
  const router = useRouter();
  const { authBusy, authError, authNotice, clearAuthNotice, currentUser, session, signIn, signUp } = useSession();
  const [mode, setMode] = useState<"signIn" | "signUp">("signIn");
  const [supportMode, setSupportMode] = useState<"resendVerification" | "forgotPassword" | "recoverAccount" | null>(null);
  const [signInValues, setSignInValues] = useState({ email: "", password: "" });
  const [signUpValues, setSignUpValues] = useState({ displayName: "", email: "", password: "", confirmPassword: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [supportEmail, setSupportEmail] = useState("");
  const [supportBusy, setSupportBusy] = useState(false);
  const [supportMessage, setSupportMessage] = useState<string | null>(null);
  const [supportError, setSupportError] = useState<string | null>(null);

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
      const result = await signIn(parsed.data);
      if (result.signedInAtUtc) {
        router.replace(nextDestination ?? "/dashboard");
      }
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
      const result = await signUp({
        displayName: parsed.data.displayName,
        email: parsed.data.email,
        password: parsed.data.password
      });
      if (result.signedInAtUtc) {
        router.replace(nextDestination ?? "/dashboard");
      }
    } catch {
    }
  }

  async function handleSupportAction(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!supportMode || !supportEmail.trim()) {
      setSupportError("Enter the email address tied to the account.");
      return;
    }

    setSupportBusy(true);
    setSupportError(null);
    setSupportMessage(null);
    clearAuthNotice();

    try {
      const normalizedEmail = supportEmail.trim();
      const result = supportMode === "resendVerification"
        ? await api.requestEmailVerification({ email: normalizedEmail })
        : supportMode === "forgotPassword"
          ? await api.requestPasswordReset({ email: normalizedEmail })
          : await api.recoverAccount({ email: normalizedEmail });

      setSupportMessage(result.message);
    } catch (error) {
      setSupportError(error instanceof Error ? error.message : "Unable to complete this request.");
    } finally {
      setSupportBusy(false);
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
                clearAuthNotice();
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
                clearAuthNotice();
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
          {authNotice ? (
            <div className="rounded-3xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">{authNotice}</div>
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
              <div className="flex flex-wrap gap-3 text-sm">
                <button
                  type="button"
                  onClick={() => {
                    setSupportMode("forgotPassword");
                    setSupportEmail(signInValues.email);
                    setSupportError(null);
                    setSupportMessage(null);
                  }}
                  className="font-semibold text-slate-700 underline decoration-slate-300 underline-offset-4 transition hover:text-slate-950"
                >
                  Forgot password?
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setSupportMode("resendVerification");
                    setSupportEmail(signInValues.email);
                    setSupportError(null);
                    setSupportMessage(null);
                  }}
                  className="font-semibold text-slate-700 underline decoration-slate-300 underline-offset-4 transition hover:text-slate-950"
                >
                  Resend verification
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setSupportMode("recoverAccount");
                    setSupportEmail(signInValues.email);
                    setSupportError(null);
                    setSupportMessage(null);
                  }}
                  className="font-semibold text-slate-700 underline decoration-slate-300 underline-offset-4 transition hover:text-slate-950"
                >
                  Recover account
                </button>
              </div>
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

          {supportMode ? (
            <Card className="border border-slate-200 bg-slate-50">
              <div className="space-y-3">
                <CardTitle className="text-xl">
                  {supportMode === "resendVerification"
                    ? "Resend verification email"
                    : supportMode === "forgotPassword"
                      ? "Reset password"
                      : "Recover account"}
                </CardTitle>
                <CardDescription>
                  {supportMode === "resendVerification"
                    ? "We’ll send a fresh email verification link if the account exists."
                    : supportMode === "forgotPassword"
                      ? "We’ll send a password reset link if the account exists and is verified."
                      : "We’ll send the safest next-step email for the account, without exposing whether it exists."}
                </CardDescription>
                {supportError ? (
                  <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{supportError}</div>
                ) : null}
                {supportMessage ? (
                  <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{supportMessage}</div>
                ) : null}
                <form className="space-y-4" onSubmit={handleSupportAction}>
                  <FieldShell htmlFor="support-email" label="Email address" required>
                    <TextInput
                      id="support-email"
                      type="email"
                      placeholder="name@example.com"
                      autoComplete="email"
                      value={supportEmail}
                      onChange={(event) => setSupportEmail(event.target.value)}
                    />
                  </FieldShell>
                  <div className="flex flex-wrap gap-3">
                    <Button type="submit" busy={supportBusy}>
                      Send email
                    </Button>
                    <Button
                      type="button"
                      variant="secondary"
                      onClick={() => {
                        setSupportMode(null);
                        setSupportError(null);
                        setSupportMessage(null);
                      }}
                    >
                      Close
                    </Button>
                  </div>
                </form>
              </div>
            </Card>
          ) : null}
        </Card>
      </div>
    </main>
  );
}
