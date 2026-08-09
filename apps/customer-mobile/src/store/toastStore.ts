import { create } from 'zustand';

export type ToastVariant = 'info' | 'success' | 'warning' | 'error';

export type ToastItem = {
  id: string;
  title: string;
  message?: string;
  variant: ToastVariant;
  durationMs: number;
};

type ToastState = {
  toasts: ToastItem[];
  show: (input: Omit<ToastItem, 'id' | 'durationMs'> & { durationMs?: number }) => void;
  dismiss: (id: string) => void;
  clear: () => void;
};

let toastSeq = 0;

export const useToastStore = create<ToastState>((set) => ({
  toasts: [],
  show: ({ title, message, variant, durationMs = 3200 }) => {
    const id = `toast-${Date.now()}-${toastSeq++}`;
    set((state) => ({
      toasts: [...state.toasts, { id, title, message, variant, durationMs }],
    }));
  },
  dismiss: (id) => set((state) => ({ toasts: state.toasts.filter((t) => t.id !== id) })),
  clear: () => set({ toasts: [] }),
}));
