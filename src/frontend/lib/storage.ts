import type { AppPreferences, AuthSession } from "@/lib/types";

const SESSION_STORAGE_KEY = "priceproof.session";
const PREFERENCES_STORAGE_KEY = "priceproof.preferences";

export const defaultPreferences: AppPreferences = {
  preferredCurrency: "ZAR",
  autoRunOcr: true,
  autoAnalyzeCase: true
};

export function readSession() {
  return readJson<AuthSession>(SESSION_STORAGE_KEY);
}

export function writeSession(session: AuthSession | null) {
  if (typeof window === "undefined") {
    return;
  }

  if (!session) {
    window.localStorage.removeItem(SESSION_STORAGE_KEY);
    return;
  }

  window.localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
}

export function readPreferences() {
  return readJson<AppPreferences>(PREFERENCES_STORAGE_KEY) ?? defaultPreferences;
}

export function writePreferences(preferences: AppPreferences) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(PREFERENCES_STORAGE_KEY, JSON.stringify(preferences));
}

function readJson<T>(key: string) {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.localStorage.getItem(key);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}
