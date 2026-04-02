"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { FieldShell, TextInput } from "@/components/ui/field";
import { api } from "@/lib/api";
import { writeSession } from "@/lib/storage";
import { flattenZodErrors, strongPasswordSchema } from "@/lib/validators";
import { z } from "zod";

const resetSchema = z.object({
  password: strongPasswordSchema,
  confirmPassword: z.string().min(1, "Confirm your password.")
}).superRefine((value, context) => {
  if (value.password !== value.confirmPassword) {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["confirmPassword"],
      message: "Passwords must match."
    });
  }
});

export function ResetPasswordScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const token = searchParams.get("token") ?? "";
  const [values, setValues] = useState({ password: "", confirmPassword: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = resetSchema.safeParse(values);

    if (!parsed.success) {
      setErrors(flattenZodErrors(parsed.error.issues));
      return;
    }

    if (!email || !token) {
      setErrors({ form: "This password reset link is incomplete. Request a fresh reset email." });
      return;
    }

    setBusy(true);
    setErrors({});
    setMessage(null);

    try {
      const session = await api.confirmPasswordReset({
        email,
        token,
        newPassword: parsed.data.password
      });

      writeSession(session);
      setMessage(session.message ?? "Password updated successfully.");
      router.push("/dashboard");
    } catch (error) {
      setErrors({ form: error instanceof Error ? error.message : "Unable to reset the password." });
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(251,191,36,0.18),transparent_24%),radial-gradient(circle_at_bottom_right,rgba(20,184,166,0.12),transparent_28%),linear-gradient(180deg,#fffdf8_0%,#f3ede1_100%)] px-4 py-6 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-2xl">
        <Card className="space-y-5">
          <CardTitle className="text-3xl">Reset password</CardTitle>
          <CardDescription>Choose a strong new password to restore access to your PriceProof SA account.</CardDescription>
          {errors.form ? (
            <div className="rounded-3xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{errors.form}</div>
          ) : null}
          {message ? (
            <div className="rounded-3xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{message}</div>
          ) : null}
          <form className="space-y-4" onSubmit={handleSubmit}>
            <FieldShell htmlFor="reset-password" label="New password" error={errors.password} required>
              <TextInput
                id="reset-password"
                type="password"
                autoComplete="new-password"
                value={values.password}
                onChange={(event) => setValues((current) => ({ ...current, password: event.target.value }))}
              />
            </FieldShell>
            <FieldShell htmlFor="reset-confirm-password" label="Confirm password" error={errors.confirmPassword} required>
              <TextInput
                id="reset-confirm-password"
                type="password"
                autoComplete="new-password"
                value={values.confirmPassword}
                onChange={(event) => setValues((current) => ({ ...current, confirmPassword: event.target.value }))}
              />
            </FieldShell>
            <Button type="submit" busy={busy}>
              Update password
            </Button>
          </form>
        </Card>
      </div>
    </main>
  );
}
