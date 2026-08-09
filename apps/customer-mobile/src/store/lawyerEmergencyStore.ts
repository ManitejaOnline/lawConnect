import { create } from 'zustand';

import type { EmergencyRequest } from '@/types/emergency';

type LawyerEmergencyState = {
  activeRequest: EmergencyRequest | null;
  setActiveRequest: (request: EmergencyRequest | null) => void;
};

export const useLawyerEmergencyStore = create<LawyerEmergencyState>((set) => ({
  activeRequest: null,
  setActiveRequest: (request) => set({ activeRequest: request }),
}));
