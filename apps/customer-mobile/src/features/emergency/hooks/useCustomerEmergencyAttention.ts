import { useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';

import { emergencyKeys } from '@/features/emergency/hooks/emergencyKeys';
import { lawyerKeys } from '@/features/lawyer/hooks/useLawyerQueries';
import { emergencyApi } from '@/services/api/emergencyApi';
import { subscribeEmergencyHub } from '@/services/realtime/emergencyHub';
import { useAuthStore } from '@/store/authStore';
import { isActionableEmergencyRequest } from '@/types/emergency';

export function useCustomerActiveSosQuery(enabled = true) {
  const userId = useAuthStore((s) => s.session?.userId);
  return useQuery({
    queryKey: emergencyKeys.customerActive(),
    queryFn: emergencyApi.getActiveSos,
    enabled: enabled && Boolean(userId),
    refetchInterval: 8_000,
    refetchOnReconnect: true,
  });
}

/** Red-dot source: server active SOS that still needs attention. */
export function useCustomerEmergencyAttention(): boolean {
  const query = useCustomerActiveSosQuery();
  return isActionableEmergencyRequest(query.data);
}

export function useCustomerEmergencyRealtime(): void {
  const queryClient = useQueryClient();

  useEffect(() => {
    const refresh = () => {
      void queryClient.invalidateQueries({ queryKey: emergencyKeys.customerActive() });
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.incomingSos() });
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.activeSos() });
    };

    return subscribeEmergencyHub({
      onStatusChanged: refresh,
      onIncomingSos: refresh,
      onAssignmentCancelled: refresh,
    });
  }, [queryClient]);
}
