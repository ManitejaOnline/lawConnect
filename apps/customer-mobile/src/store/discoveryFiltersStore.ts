import { create } from 'zustand';

import type { DiscoveryFilters } from '@/types/home';

export const DEFAULT_DISCOVERY_FILTERS: DiscoveryFilters = {
  sortBy: 'relevance',
  sortDescending: false,
};

type DiscoveryFiltersState = {
  draft: DiscoveryFilters;
  applied: DiscoveryFilters;
  setDraft: (patch: Partial<DiscoveryFilters>) => void;
  replaceDraft: (next: DiscoveryFilters) => void;
  applyDraft: () => void;
  resetDraft: () => void;
  resetAll: () => void;
  activeFilterCount: () => number;
};

function countActive(filters: DiscoveryFilters): number {
  let count = 0;
  if (filters.specializationId) count += 1;
  if (filters.languageCode) count += 1;
  if (filters.minRating != null) count += 1;
  if (filters.minFee != null || filters.maxFee != null) count += 1;
  if (filters.minExperience != null) count += 1;
  if (filters.maxDistanceKm != null) count += 1;
  if (filters.isOnline) count += 1;
  if (filters.isAvailableForEmergency) count += 1;
  if (filters.sortBy !== 'relevance') count += 1;
  return count;
}

export const useDiscoveryFiltersStore = create<DiscoveryFiltersState>((set, get) => ({
  draft: { ...DEFAULT_DISCOVERY_FILTERS },
  applied: { ...DEFAULT_DISCOVERY_FILTERS },
  setDraft: (patch) => set((state) => ({ draft: { ...state.draft, ...patch } })),
  replaceDraft: (next) => set({ draft: next }),
  applyDraft: () => set((state) => ({ applied: { ...state.draft } })),
  resetDraft: () => set({ draft: { ...DEFAULT_DISCOVERY_FILTERS } }),
  resetAll: () =>
    set({
      draft: { ...DEFAULT_DISCOVERY_FILTERS },
      applied: { ...DEFAULT_DISCOVERY_FILTERS },
    }),
  activeFilterCount: () => countActive(get().applied),
}));

export const LANGUAGE_OPTIONS = [
  { code: 'en', label: 'English' },
  { code: 'hi', label: 'Hindi' },
  { code: 'ta', label: 'Tamil' },
  { code: 'te', label: 'Telugu' },
  { code: 'mr', label: 'Marathi' },
  { code: 'bn', label: 'Bengali' },
  { code: 'gu', label: 'Gujarati' },
  { code: 'kn', label: 'Kannada' },
  { code: 'ml', label: 'Malayalam' },
  { code: 'pa', label: 'Punjabi' },
] as const;

export const EXPERIENCE_OPTIONS = [
  { value: 1, label: '1+ yrs' },
  { value: 3, label: '3+ yrs' },
  { value: 5, label: '5+ yrs' },
  { value: 10, label: '10+ yrs' },
  { value: 15, label: '15+ yrs' },
] as const;

export const RATING_OPTIONS = [
  { value: 3, label: '3.0+' },
  { value: 3.5, label: '3.5+' },
  { value: 4, label: '4.0+' },
  { value: 4.5, label: '4.5+' },
] as const;

export const FEE_PRESETS = [
  { min: undefined, max: 1000, label: 'Under ₹1k' },
  { min: 1000, max: 2500, label: '₹1k–2.5k' },
  { min: 2500, max: 5000, label: '₹2.5k–5k' },
  { min: 5000, max: undefined, label: '₹5k+' },
] as const;

export const DISTANCE_OPTIONS = [
  { value: 5, label: '5 km' },
  { value: 10, label: '10 km' },
  { value: 25, label: '25 km' },
  { value: 50, label: '50 km' },
  { value: 100, label: '100 km' },
] as const;

export const SORT_OPTIONS = [
  { value: 'relevance' as const, label: 'Relevance', descending: false },
  { value: 'rating' as const, label: 'Top rated', descending: true },
  { value: 'fee' as const, label: 'Fee: low to high', descending: false },
  { value: 'fee' as const, label: 'Fee: high to low', descending: true },
  { value: 'experience' as const, label: 'Most experienced', descending: true },
  { value: 'distance' as const, label: 'Nearest', descending: false },
];
