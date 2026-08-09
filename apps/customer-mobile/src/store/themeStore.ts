import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import type { ThemeMode } from '@/theme';
import { mmkvLikeStorage } from '@/services/storage/asyncJsonStorage';

type ThemeState = {
  mode: ThemeMode;
  setMode: (mode: ThemeMode) => void;
};

export const useThemeStore = create<ThemeState>()(
  persist(
    (set) => ({
      mode: 'light',
      setMode: (mode) => set({ mode }),
    }),
    {
      name: 'lawconnect.theme',
      storage: createJSONStorage(() => mmkvLikeStorage),
    },
  ),
);
