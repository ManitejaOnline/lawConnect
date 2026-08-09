import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useDeviceLocation } from '@/hooks/useDeviceLocation';
import { appointmentKeys } from '@/features/appointments/hooks/useAppointmentQueries';
import { appointmentsApi, lawyersApi } from '@/services/api/lawyersApi';
import { useDiscoveryFiltersStore } from '@/store/discoveryFiltersStore';
import type { DiscoveryFilters, LawyerSearchParams } from '@/types/home';

export const discoveryKeys = {
  all: ['discovery'] as const,
  search: (params: Omit<LawyerSearchParams, 'pageNumber'>) =>
    [...discoveryKeys.all, 'search', params] as const,
  profile: (lawyerId: string) => [...discoveryKeys.all, 'profile', lawyerId] as const,
  slots: (lawyerId: string, date: string) =>
    [...discoveryKeys.all, 'slots', lawyerId, date] as const,
};

function filtersToParams(
  filters: DiscoveryFilters,
  extras: { search?: string; originLatitude?: number; originLongitude?: number },
): Omit<LawyerSearchParams, 'pageNumber'> {
  return {
    search: extras.search,
    specializationId: filters.specializationId,
    languageCode: filters.languageCode,
    minRating: filters.minRating,
    minFee: filters.minFee,
    maxFee: filters.maxFee,
    minExperience: filters.minExperience,
    maxDistanceKm: filters.maxDistanceKm,
    isOnline: filters.isOnline || undefined,
    isAvailableForEmergency: filters.isAvailableForEmergency || undefined,
    sortBy: filters.sortBy,
    sortDescending: filters.sortDescending,
    originLatitude: extras.originLatitude,
    originLongitude: extras.originLongitude,
    pageSize: 15,
  };
}

export function useInfiniteLawyerSearch(searchText?: string) {
  const applied = useDiscoveryFiltersStore((s) => s.applied);
  const { location } = useDeviceLocation();

  const needsLocation =
    applied.sortBy === 'distance' || applied.maxDistanceKm != null;

  const baseParams = filtersToParams(applied, {
    search: searchText?.trim() || undefined,
    originLatitude: needsLocation ? location.latitude : undefined,
    originLongitude: needsLocation ? location.longitude : undefined,
  });

  return useInfiniteQuery({
    queryKey: discoveryKeys.search(baseParams),
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      lawyersApi.search({
        ...baseParams,
        pageNumber: pageParam,
      }),
    getNextPageParam: (lastPage) => (lastPage.hasNextPage ? lastPage.pageNumber + 1 : undefined),
    staleTime: 15_000,
    gcTime: 5 * 60_000,
    refetchInterval: 15_000,
  });
}

export function useLawyerProfileQuery(lawyerId: string) {
  return useQuery({
    queryKey: discoveryKeys.profile(lawyerId),
    queryFn: () => lawyersApi.getById(lawyerId),
    enabled: Boolean(lawyerId),
    staleTime: 15_000,
    refetchInterval: 15_000,
  });
}

export function useLawyerSlotsQuery(lawyerId: string, date: string, enabled = true) {
  return useQuery({
    queryKey: discoveryKeys.slots(lawyerId, date),
    queryFn: () => lawyersApi.getAvailableSlots(lawyerId, date),
    enabled: Boolean(lawyerId && date && enabled),
    staleTime: 15_000,
  });
}

export function useBookAppointmentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: appointmentsApi.book,
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: discoveryKeys.slots(variables.lawyerId, variables.startUtc.slice(0, 10)),
      });
      void queryClient.invalidateQueries({ queryKey: appointmentKeys.all });
    },
  });
}
