import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { appointmentsApi } from '@/services/api/lawyersApi';
import type { Appointment } from '@/types/appointment';

export const appointmentKeys = {
  all: ['appointments'] as const,
  upcoming: () => [...appointmentKeys.all, 'upcoming'] as const,
  history: () => [...appointmentKeys.all, 'history'] as const,
  detail: (id: string) => [...appointmentKeys.all, 'detail', id] as const,
};

const POLL_MS = 12_000;

export function useUpcomingAppointmentsQuery() {
  return useQuery({
    queryKey: appointmentKeys.upcoming(),
    queryFn: () => appointmentsApi.getUpcoming(1, 50),
    refetchInterval: POLL_MS,
    refetchOnReconnect: true,
  });
}

export function useAppointmentHistoryQuery() {
  return useQuery({
    queryKey: appointmentKeys.history(),
    queryFn: () => appointmentsApi.getHistory(1, 50),
    refetchInterval: POLL_MS,
    refetchOnReconnect: true,
  });
}

export function useAppointmentQuery(appointmentId: string) {
  return useQuery({
    queryKey: appointmentKeys.detail(appointmentId),
    queryFn: () => appointmentsApi.getById(appointmentId),
    enabled: Boolean(appointmentId),
    refetchInterval: POLL_MS,
  });
}

export function useCancelAppointmentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ appointmentId, reason }: { appointmentId: string; reason?: string }) =>
      appointmentsApi.cancel(appointmentId, reason),
    onSuccess: (appointment: Appointment) => {
      void queryClient.invalidateQueries({ queryKey: appointmentKeys.all });
      queryClient.setQueryData(appointmentKeys.detail(appointment.id), appointment);
    },
  });
}
