"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { api } from "@/lib/api";
import { defaultPreferences, readPreferences, readSession, writePreferences, writeSession } from "@/lib/storage";
import type { AppPreferences, AuthSession, CurrentUser, SignInRequest, SignUpRequest } from "@/lib/types";

type SessionContextValue = {
  session: AuthSession | null;
  currentUser: CurrentUser | null;
  preferences: AppPreferences;
  initializing: boolean;
  authBusy: boolean;
  authError: string | null;
  authNotice: string | null;
  signIn: (input: SignInRequest) => Promise<AuthSession>;
  signUp: (input: SignUpRequest) => Promise<AuthSession>;
  signOut: () => Promise<void>;
  refreshCurrentUser: () => Promise<void>;
  updatePreferences: (next: AppPreferences) => void;
  clearAuthNotice: () => void;
};

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [preferences, setPreferences] = useState<AppPreferences>(defaultPreferences);
  const [initializing, setInitializing] = useState(true);
  const [authBusy, setAuthBusy] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);
  const [authNotice, setAuthNotice] = useState<string | null>(null);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    let isCancelled = false;
    const storedPreferences = readPreferences();
    const storedSession = readSession();

    setPreferences(storedPreferences);
    setSession(storedSession?.signedInAtUtc ? storedSession : null);
    setHydrated(true);

    if (!storedSession?.signedInAtUtc) {
      setInitializing(false);
      return () => {
        isCancelled = true;
      };
    }

    void (async () => {
      try {
        const user = await api.getCurrentUser();

        if (!isCancelled) {
          setCurrentUser(user);
        }
      } catch {
        if (!isCancelled) {
          setSession(null);
          setCurrentUser(null);
          writeSession(null);
        }
      } finally {
        if (!isCancelled) {
          setInitializing(false);
        }
      }
    })();

    return () => {
      isCancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!hydrated) {
      return;
    }

    writeSession(session);
  }, [hydrated, session]);

  useEffect(() => {
    if (!hydrated) {
      return;
    }

    writePreferences(preferences);
  }, [hydrated, preferences]);

  async function completeAuth(action: Promise<AuthSession>) {
    setAuthBusy(true);
    setAuthError(null);
    setAuthNotice(null);

    try {
      const nextSession = await action;
      setAuthNotice(nextSession.message ?? null);

      if (nextSession.signedInAtUtc) {
        const nextUser = await api.getCurrentUser();
        setSession(nextSession);
        setCurrentUser(nextUser);
      } else {
        setSession(null);
        setCurrentUser(null);
      }

      return nextSession;
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unable to complete your request.";
      setAuthError(message);
      throw error;
    } finally {
      setAuthBusy(false);
    }
  }

  async function refreshCurrentUser() {
    if (!session) {
      return;
    }

    const nextUser = await api.getCurrentUser();
    setCurrentUser(nextUser);
  }

  async function signOut() {
    try {
      await api.signOut();
    } catch {
    } finally {
      setSession(null);
      setCurrentUser(null);
      setAuthError(null);
      setAuthNotice(null);
    }
  }

  function updatePreferences(next: AppPreferences) {
    setPreferences(next);
  }

  return (
    <SessionContext.Provider
      value={{
        session,
        currentUser,
        preferences,
        initializing,
        authBusy,
        authError,
        authNotice,
        signIn: (input) => completeAuth(api.signIn(input)),
        signUp: (input) => completeAuth(api.signUp(input)),
        signOut,
        refreshCurrentUser,
        updatePreferences,
        clearAuthNotice: () => setAuthNotice(null)
      }}
    >
      {children}
    </SessionContext.Provider>
  );
}

export function useSession() {
  const context = useContext(SessionContext);

  if (!context) {
    throw new Error("useSession must be used within SessionProvider.");
  }

  return context;
}
