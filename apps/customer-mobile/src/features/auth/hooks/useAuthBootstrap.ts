import React, { useEffect, useState } from 'react';

import { useAuthStore } from '@/store/authStore';
import { authApi } from '@/services/api/authApi';

const MIN_SPLASH_MS = 1100;

/**
 * Hydrates SecureStore session, refreshes tokens, and enforces a minimum splash duration.
 */
export function useAuthBootstrap() {
  const hydrated = useAuthStore((s) => s.hydrated);
  const sessionRestored = useAuthStore((s) => s.sessionRestored);
  const setHydrated = useAuthStore((s) => s.setHydrated);
  const setSessionRestored = useAuthStore((s) => s.setSessionRestored);
  const [minTimeElapsed, setMinTimeElapsed] = useState(false);
  const [statusLabel, setStatusLabel] = useState('Starting…');

  useEffect(() => {
    const safety = setTimeout(() => {
      if (!useAuthStore.getState().hydrated) {
        setHydrated(true);
      }
    }, 1500);
    return () => clearTimeout(safety);
  }, [setHydrated]);

  useEffect(() => {
    const timer = setTimeout(() => setMinTimeElapsed(true), MIN_SPLASH_MS);
    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (!hydrated || sessionRestored) return;

    let cancelled = false;

    const run = async () => {
      const session = useAuthStore.getState().session;
      if (!session?.refreshToken) {
        if (!cancelled) {
          setStatusLabel('Ready');
          setSessionRestored(true);
        }
        return;
      }

      if (!cancelled) setStatusLabel('Restoring your session…');
      const ok = await authApi.restoreSession();
      if (!cancelled) {
        setStatusLabel(ok ? 'Welcome back' : 'Session expired');
        setSessionRestored(true);
      }
    };

    void run();
    return () => {
      cancelled = true;
    };
  }, [hydrated, sessionRestored, setSessionRestored]);

  return {
    ready: hydrated && sessionRestored && minTimeElapsed,
    statusLabel,
    restoring: hydrated && !sessionRestored,
  };
}
