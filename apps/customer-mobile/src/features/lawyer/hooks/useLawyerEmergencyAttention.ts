import {
  useIncomingSosQuery,
  useLawyerActiveSosQuery,
} from '@/features/lawyer/hooks/useLawyerQueries';
import {
  isActionableEmergencyRequest,
  isIncomingAssignmentActionable,
} from '@/types/emergency';

/** Lawyer red dot: incoming assignment waiting, or an active SOS still in progress. */
export function useLawyerEmergencyAttention(): boolean {
  const incoming = useIncomingSosQuery();
  const active = useLawyerActiveSosQuery();

  const hasIncoming = (incoming.data ?? []).some(isIncomingAssignmentActionable);
  const hasActive = isActionableEmergencyRequest(active.data);
  return hasIncoming || hasActive;
}
