import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';

import { emergencyKeys } from '@/features/emergency/hooks/emergencyKeys';
import { useChatInboxRealtime } from '@/features/chat/hooks/useConversations';
import { lawyerKeys } from '@/features/lawyer/hooks/useLawyerQueries';
import { subscribeEmergencyHub } from '@/services/realtime/emergencyHub';
import type { IncomingEmergencySos } from '@/types/emergency';

/** Chat inbox + emergency IncomingSos for the lawyer shell. */
export function useLawyerWorkspaceRealtime() {
  const queryClient = useQueryClient();
  useChatInboxRealtime();

  useEffect(() => {
    const refreshAttention = () => {
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.incomingSos() });
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.activeSos() });
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.history() });
      void queryClient.invalidateQueries({ queryKey: emergencyKeys.customerActive() });
    };

    return subscribeEmergencyHub({
      onIncomingSos: (item: IncomingEmergencySos) => {
        queryClient.setQueryData<IncomingEmergencySos[]>(lawyerKeys.incomingSos(), (prev) => {
          const list = prev ?? [];
          if (list.some((row) => row.assignmentId === item.assignmentId)) {
            return list.map((row) => (row.assignmentId === item.assignmentId ? item : row));
          }
          return [item, ...list];
        });
        refreshAttention();
      },
      onAssignmentCancelled: (payload) => {
        queryClient.setQueryData<IncomingEmergencySos[]>(lawyerKeys.incomingSos(), (prev) =>
          (prev ?? []).filter((row) => row.assignmentId !== payload.assignmentId),
        );
        refreshAttention();
      },
      onStatusChanged: (request) => {
        refreshAttention();
        if (request?.requestId) {
          queryClient.setQueryData(lawyerKeys.sos(request.requestId), request);
        }
      },
    });
  }, [queryClient]);
}
