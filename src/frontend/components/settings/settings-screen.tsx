"use client";

import { useState } from "react";
import { getApiBaseUrl } from "@/lib/api";
import { preferencesSchema } from "@/lib/validators";
import { useSession } from "@/components/providers/session-provider";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { CheckboxField, FieldShell, TextInput } from "@/components/ui/field";
import { PageHeader } from "@/components/ui/page-header";

export function SettingsScreen() {
  const { currentUser, preferences, session, signOut, updatePreferences } = useSession();
  const [formValues, setFormValues] = useState(preferences);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  function savePreferences() {
    const parsed = preferencesSchema.safeParse(formValues);

    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? "Check the settings values and try again.");
      setMessage(null);
      return;
    }

    updatePreferences(parsed.data);
    setMessage("Local preferences updated for this device.");
    setError(null);
  }

  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Preferences" title="Settings" description="Review your current session and adjust local-device workflow defaults that the wizard uses." />

      {message ? <Card className="border-emerald-200 bg-emerald-50"><CardDescription className="text-emerald-800">{message}</CardDescription></Card> : null}
      {error ? <Card className="border-rose-200 bg-rose-50"><CardDescription className="text-rose-800">{error}</CardDescription></Card> : null}

      <div className="grid gap-6 lg:grid-cols-[0.9fr_1.1fr]">
        <Card className="space-y-4">
          <CardTitle>Session details</CardTitle>
          <div className="space-y-3 text-sm text-slate-700">
            <p><span className="font-semibold text-slate-950">Display name:</span> {currentUser?.displayName ?? session?.displayName}</p>
            <p><span className="font-semibold text-slate-950">Email:</span> {currentUser?.email ?? session?.email}</p>
            <p><span className="font-semibold text-slate-950">API base URL:</span> {getApiBaseUrl()}</p>
          </div>
          <Button variant="secondary" onClick={signOut}>Sign out</Button>
        </Card>

        <Card className="space-y-4">
          <CardTitle>Local workflow defaults</CardTitle>
          <CardDescription>
            These preferences are stored in your browser on this device. They do not change server-side account settings.
          </CardDescription>
          <FieldShell htmlFor="preferred-currency" label="Preferred currency code">
            <TextInput
              id="preferred-currency"
              value={formValues.preferredCurrency}
              onChange={(event) => setFormValues((current) => ({ ...current, preferredCurrency: event.target.value.toUpperCase() }))}
            />
          </FieldShell>
          <div className="grid gap-3">
            <CheckboxField id="auto-run-ocr" label="Run OCR automatically after receipt upload" checked={formValues.autoRunOcr} onChange={(checked) => setFormValues((current) => ({ ...current, autoRunOcr: checked }))} />
            <CheckboxField id="auto-analyze" label="Run discrepancy analysis automatically after submission" checked={formValues.autoAnalyzeCase} onChange={(checked) => setFormValues((current) => ({ ...current, autoAnalyzeCase: checked }))} />
          </div>
          <Button onClick={savePreferences}>Save preferences</Button>
        </Card>
      </div>
    </div>
  );
}
