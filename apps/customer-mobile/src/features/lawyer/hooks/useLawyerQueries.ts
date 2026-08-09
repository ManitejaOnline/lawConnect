import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { discoveryKeys } from '@/features/discovery/hooks/useDiscoveryQueries';
import { emergencyApi } from '@/services/api/emergencyApi';
import { lawyersApi } from '@/services/api/lawyersApi';
import { useAuthStore } from '@/store/authStore';
import type { IncomingEmergencySos } from '@/types/emergency';
import type { LawyerProfile } from '@/types/home';

export const lawyerKeys = {
  me: (userId?: string | null) => ['lawyers', 'me', userId] as const,
  availability: (userId?: string | null) => ['lawyers', 'me', 'availability', userId] as const,
  incomingSos: () => ['emergency', 'incoming'] as const,
  activeSos: () => ['emergency', 'lawyer-active'] as const,
  history: () => ['emergency', 'lawyer-history'] as const,
  sos: (requestId: string) => ['emergency', 'sos', requestId] as const,
};

export function useMyLawyerProfileQuery() {
  const userId = useAuthStore((s) => s.session?.userId);
  return useQuery({
    queryKey: lawyerKeys.me(userId),
    queryFn: lawyersApi.getMine,
    enabled: Boolean(userId),
    refetchInterval: 20_000,
  });
}

export function useIncomingSosQuery(enabled = true) {
  return useQuery({
    queryKey: lawyerKeys.incomingSos(),
    queryFn: emergencyApi.getIncoming,
    enabled,
    refetchInterval: 5_000,
    refetchOnReconnect: true,
  });
}

export function useLawyerActiveSosQuery(enabled = true) {
  return useQuery({
    queryKey: lawyerKeys.activeSos(),
    queryFn: emergencyApi.getLawyerActiveSos,
    enabled,
    refetchInterval: 8_000,
    refetchOnReconnect: true,
  });
}

export function useLawyerEmergencyHistoryQuery(enabled = true) {
  return useQuery({
    queryKey: lawyerKeys.history(),
    queryFn: () => emergencyApi.getHistory(1, 30),
    enabled,
  });
}

export function useSetOnlineStatusMutation() {
  const queryClient = useQueryClient();
  const userId = useAuthStore((s) => s.session?.userId);
  return useMutation({
    mutationFn: (isOnline: boolean) => lawyersApi.setOnlineStatus(isOnline),
    onSuccess: (profile: LawyerProfile) => {
      queryClient.setQueryData(lawyerKeys.me(userId), profile);
      void queryClient.invalidateQueries({ queryKey: discoveryKeys.all });
    },
  });
}

export function useSetEmergencyAvailabilityMutation() {
  const queryClient = useQueryClient();
  const userId = useAuthStore((s) => s.session?.userId);
  return useMutation({
    mutationFn: (isAvailableForEmergency: boolean) => {
      const current = queryClient.getQueryData<LawyerProfile>(lawyerKeys.me(userId));
      return lawyersApi.setEmergencyAvailability({
        isAvailableForEmergency,
        emergencyFee: current?.emergencyFee ?? null,
      });
    },
    onSuccess: (profile: LawyerProfile) => {
      queryClient.setQueryData(lawyerKeys.me(userId), profile);
      void queryClient.invalidateQueries({ queryKey: discoveryKeys.all });
    },
  });
}

export function useUpdateLawyerFeesMutation() {
  const queryClient = useQueryClient();
  const userId = useAuthStore((s) => s.session?.userId);
  return useMutation({
    mutationFn: lawyersApi.updateFees,
    onSuccess: (profile: LawyerProfile) => {
      queryClient.setQueryData(lawyerKeys.me(userId), profile);
    },
  });
}

export function useUpdateLawyerProfileMutation() {
  const queryClient = useQueryClient();
  const userId = useAuthStore((s) => s.session?.userId);
  return useMutation({
    mutationFn: lawyersApi.updateProfile,
    onSuccess: (profile: LawyerProfile) => {
      queryClient.setQueryData(lawyerKeys.me(userId), profile);
    },
  });
}

export function useAcceptSosMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (assignmentId: string) => emergencyApi.acceptAssignment(assignmentId),
    onSuccess: (request) => {
      queryClient.setQueryData(lawyerKeys.activeSos(), request);
      queryClient.setQueryData(lawyerKeys.sos(request.requestId), request);
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.incomingSos() });
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.activeSos() });
    },
  });
}

export function useDeclineSosMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (assignmentId: string) => emergencyApi.declineAssignment(assignmentId),
    onSuccess: (_void, assignmentId) => {
      queryClient.setQueryData<IncomingEmergencySos[]>(lawyerKeys.incomingSos(), (prev) =>
        (prev ?? []).filter((row) => row.assignmentId !== assignmentId),
      );
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.incomingSos() });
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.activeSos() });
    },
  });
}

export function useMyAvailabilityQuery() {
  const userId = useAuthStore((s) => s.session?.userId);
  return useQuery({
    queryKey: lawyerKeys.availability(userId),
    queryFn: lawyersApi.getMyAvailability,
    enabled: Boolean(userId),
  });
}

export function useSetMyAvailabilityMutation() {
  const queryClient = useQueryClient();
  const userId = useAuthStore((s) => s.session?.userId);
  return useMutation({
    mutationFn: lawyersApi.setMyAvailability,
    onSuccess: (slots) => {
      queryClient.setQueryData(lawyerKeys.availability(userId), slots);
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.me(userId) });
      void queryClient.invalidateQueries({ queryKey: discoveryKeys.all });
    },
  });
}
