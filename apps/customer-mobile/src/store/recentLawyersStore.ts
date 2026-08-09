import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import { mmkvLikeStorage } from '@/services/storage/asyncJsonStorage';
import type { LawyerSearchItem } from '@/types/home';

const MAX_RECENT = 12;

type RecentLawyersState = {
  recent: LawyerSearchItem[];
  addRecent: (lawyer: LawyerSearchItem) => void;
  clearRecent: () => void;
};

export const useRecentLawyersStore = create<RecentLawyersState>()(
  persist(
    (set, get) => ({
      recent: [],
      addRecent: (lawyer) => {
        const next = [lawyer, ...get().recent.filter((item) => item.lawyerId !== lawyer.lawyerId)].slice(
          0,
          MAX_RECENT,
        );
        set({ recent: next });
      },
      clearRecent: () => set({ recent: [] }),
    }),
    {
      name: 'lawconnect.recent-lawyers',
      storage: createJSONStorage(() => mmkvLikeStorage),
    },
  ),
);
