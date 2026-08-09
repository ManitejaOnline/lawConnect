import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import type { AppAudience } from '@/auth/roles';
import { mmkvLikeStorage } from '@/services/storage/asyncJsonStorage';

export type AuthSession = {
  userId: string;
  phoneNumber: string;
  roles: string[];
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
};

export type AuthProfile = {
  userId: string;
  phoneNumber: string;
  firstName: string;
  lastName: string;
  roles: string[];
};

type AuthState = {
  hydrated: boolean;
  sessionRestored: boolean;
  onboardingCompleted: boolean;
  intendedAudience: AppAudience | null;
  session: AuthSession | null;
  profile: AuthProfile | null;
  deviceIdentifier: string | null;
  setHydrated: (value: boolean) => void;
  setSessionRestored: (value: boolean) => void;
  setOnboardingCompleted: (value: boolean) => void;
  setIntendedAudience: (audience: AppAudience | null) => void;
  setSession: (session: AuthSession | null) => void;
  setProfile: (profile: AuthProfile | null) => void;
  setDeviceIdentifier: (id: string) => void;
  clearSession: () => void;
  isAuthenticated: () => boolean;
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      hydrated: false,
      sessionRestored: false,
      onboardingCompleted: false,
      intendedAudience: null,
      session: null,
      profile: null,
      deviceIdentifier: null,
      setHydrated: (value) => set({ hydrated: value }),
      setSessionRestored: (value) => set({ sessionRestored: value }),
      setOnboardingCompleted: (value) => set({ onboardingCompleted: value }),
      setIntendedAudience: (audience) => set({ intendedAudience: audience }),
      setSession: (session) => set({ session }),
      setProfile: (profile) => set({ profile }),
      setDeviceIdentifier: (id) => set({ deviceIdentifier: id }),
      clearSession: () => set({ session: null, profile: null }),
      isAuthenticated: () => Boolean(get().session?.accessToken),
    }),
    {
      name: 'lawconnect.auth',
      storage: createJSONStorage(() => mmkvLikeStorage),
      partialize: (state) => ({
        session: state.session,
        profile: state.profile,
        deviceIdentifier: state.deviceIdentifier,
        onboardingCompleted: state.onboardingCompleted,
        intendedAudience: state.intendedAudience,
      }),
      onRehydrateStorage: () => (state) => {
        state?.setHydrated(true);
      },
    },
  ),
);
