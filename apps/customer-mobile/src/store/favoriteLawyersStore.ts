import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import { mmkvLikeStorage } from '@/services/storage/asyncJsonStorage';

type FavoriteLawyersState = {
  ids: string[];
  toggle: (lawyerId: string) => void;
  isFavorite: (lawyerId: string) => boolean;
};

export const useFavoriteLawyersStore = create<FavoriteLawyersState>()(
  persist(
    (set, get) => ({
      ids: [],
      toggle: (lawyerId) =>
        set((state) => ({
          ids: state.ids.includes(lawyerId)
            ? state.ids.filter((id) => id !== lawyerId)
            : [...state.ids, lawyerId],
        })),
      isFavorite: (lawyerId) => get().ids.includes(lawyerId),
    }),
    {
      name: 'lawconnect.favorite-lawyers',
      storage: createJSONStorage(() => mmkvLikeStorage),
    },
  ),
);
