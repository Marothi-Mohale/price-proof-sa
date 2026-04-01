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
  signIn: (input: SignInRequest) => Promise<AuthSession>;
  signUp: (input: SignUpRequest) => Promise<AuthSession>;
  signOut: () => void;
  refreshCurrentUser: () => Promise<void>;
  updatePreferences: (next: AppPreferences) => void;
};

const SessionContext = createContext<SessionContextValue | undefined>(undefined);

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [preferences, setPreferences] = useState<AppPreferences>(defaultPreferences);
  const [initializing, setInitializing] = useState(true);
  const [authBusy, setAuthBusy] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    let isCancelled = false;
    const storedPreferences = readPreferences();
    const storedSession = readSession();

    setPreferences(storedPreferences);
    setSession(storedSession);
    setHydrated(true);

    if (!storedSession) {
      setInitializing(false);
      return () => {
        isCancelled = true;
      };
    }

    void (async () => {
      try {
        const user = await api.getCurrentUser(storedSession.userId);

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

    try {
      const nextSession = await action;
      const nextUser = await api.getCurrentUser(nextSession.userId);

      setSession(nextSession);
      setCurrentUser(nextUser);

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

    const nextUser = await api.getCurrentUser(session.userId);
    setCurrentUser(nextUser);
  }

  function signOut() {
    setSession(null);
    setCurrentUser(null);
    setAuthError(null);
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
        signIn: (input) => completeAuth(api.signIn(input)),
        signUp: (input) => completeAuth(api.signUp(input)),
        signOut,
        refreshCurrentUser,
        updatePreferences
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
