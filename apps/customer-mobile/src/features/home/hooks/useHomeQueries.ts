import { useQuery } from '@tanstack/react-query';

import { emergencyApi } from '@/services/api/emergencyApi';
import { lawyersApi } from '@/services/api/lawyersApi';
import { notificationsApi } from '@/services/api/notificationsApi';
import { usersApi } from '@/services/api/usersApi';
import type { LawyerSearchParams } from '@/types/home';
import type { DeviceLocation } from '@/hooks/useDeviceLocation';

export const homeQueryKeys = {
  all: ['home'] as const,
  featured: () => [...homeQueryKeys.all, 'featured'] as const,
  topRated: (pageSize = 10) => [...homeQueryKeys.all, 'topRated', pageSize] as const,
  nearby: (lat: number, lng: number, pageSize = 10) =>
    [...homeQueryKeys.all, 'nearby', lat, lng, pageSize] as const,
  search: (params: LawyerSearchParams) => [...homeQueryKeys.all, 'search', params] as const,
  notifications: () => [...homeQueryKeys.all, 'notifications'] as const,
  emergencyCategories: () => [...homeQueryKeys.all, 'emergencyCategories'] as const,
  profile: () => [...homeQueryKeys.all, 'profile'] as const,
};

export function useFeaturedLawyerQuery() {
  return useQuery({
    queryKey: homeQueryKeys.featured(),
    queryFn: async () => {
      const page = await lawyersApi.search({
        sortBy: 'rating',
        sortDescending: true,
        minRating: 4,
        pageSize: 1,
        pageNumber: 1,
      });
      return page.items[0] ?? null;
    },
  });
}

export function useTopRatedLawyersQuery(pageSize = 10) {
  return useQuery({
    queryKey: homeQueryKeys.topRated(pageSize),
    queryFn: () =>
      lawyersApi.search({
        sortBy: 'rating',
        sortDescending: true,
        pageSize,
        pageNumber: 1,
      }),
  });
}

export function useNearbyLawyersQuery(location: DeviceLocation, pageSize = 10, maxDistanceKm = 50) {
  return useQuery({
    queryKey: homeQueryKeys.nearby(location.latitude, location.longitude, pageSize),
    queryFn: () =>
      lawyersApi.search({
        originLatitude: location.latitude,
        originLongitude: location.longitude,
        maxDistanceKm,
        sortBy: 'distance',
        sortDescending: false,
        pageSize,
        pageNumber: 1,
      }),
  });
}

export function useLawyerSearchQuery(params: LawyerSearchParams, enabled = true) {
  return useQuery({
    queryKey: homeQueryKeys.search(params),
    queryFn: () => lawyersApi.search(params),
    enabled,
  });
}

export function useNotificationsQuery() {
  return useQuery({
    queryKey: homeQueryKeys.notifications(),
    queryFn: () => notificationsApi.list(1, 30, false),
  });
}

export function useEmergencyCategoriesQuery(enabled = true) {
  return useQuery({
    queryKey: homeQueryKeys.emergencyCategories(),
    queryFn: () => emergencyApi.getCategories(),
    enabled,
  });
}

export function useUserProfileQuery() {
  return useQuery({
    queryKey: homeQueryKeys.profile(),
    queryFn: () => usersApi.me(),
  });
}
