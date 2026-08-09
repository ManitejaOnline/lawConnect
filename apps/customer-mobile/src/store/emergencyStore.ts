import { create } from 'zustand';

import type {
  EmergencyErrorKind,
  EmergencyRequest,
  EmergencySessionPhase,
  SosCountdownPayload,
} from '@/types/emergency';
import type { LawyerProfile } from '@/types/home';

type EmergencyState = {
  phase: EmergencySessionPhase;
  request: EmergencyRequest | null;
  assignedLawyer: LawyerProfile | null;
  countdown: SosCountdownPayload | null;
  statusMessages: string[];
  errorKind: EmergencyErrorKind | null;
  errorMessage: string | null;
  selectedCategoryId: string | null;
  selectedCategoryTitle: string | null;
  callMode: 'voice' | 'video';
  rating: number;
  setPhase: (phase: EmergencySessionPhase) => void;
  setRequest: (request: EmergencyRequest | null) => void;
  setAssignedLawyer: (lawyer: LawyerProfile | null) => void;
  setCountdown: (countdown: SosCountdownPayload | null) => void;
  pushStatus: (message: string) => void;
  setError: (kind: EmergencyErrorKind, message: string) => void;
  clearError: () => void;
  setCategory: (id: string, title: string) => void;
  setCallMode: (mode: 'voice' | 'video') => void;
  setRating: (value: number) => void;
  resetSession: () => void;
};

const initial = {
  phase: 'idle' as EmergencySessionPhase,
  request: null,
  assignedLawyer: null,
  countdown: null,
  statusMessages: [] as string[],
  errorKind: null,
  errorMessage: null,
  selectedCategoryId: null,
  selectedCategoryTitle: null,
  callMode: 'voice' as const,
  rating: 0,
};

export const useEmergencyStore = create<EmergencyState>((set) => ({
  ...initial,
  setPhase: (phase) => set({ phase }),
  setRequest: (request) => set({ request }),
  setAssignedLawyer: (assignedLawyer) => set({ assignedLawyer }),
  setCountdown: (countdown) => set({ countdown }),
  pushStatus: (message) =>
    set((state) => ({
      statusMessages: [message, ...state.statusMessages].slice(0, 8),
    })),
  setError: (errorKind, errorMessage) => set({ phase: 'error', errorKind, errorMessage }),
  clearError: () => set({ errorKind: null, errorMessage: null }),
  setCategory: (selectedCategoryId, selectedCategoryTitle) =>
    set({ selectedCategoryId, selectedCategoryTitle }),
  setCallMode: (callMode) => set({ callMode }),
  setRating: (rating) => set({ rating }),
  resetSession: () => set({ ...initial }),
}));
