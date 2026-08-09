import { useCallback, useEffect, useRef } from 'react';
import { AppState, type AppStateStatus } from 'react-native';

import { emergencyApi } from '@/services/api/emergencyApi';
import { lawyersApi } from '@/services/api/lawyersApi';
import {
  ensureEmergencyHub,
  joinEmergencyRequest,
  leaveEmergencyRequest,
  stopEmergencyHub,
  subscribeEmergencyHub,
} from '@/services/realtime/emergencyHub';
import { emergencyKeys } from '@/features/emergency/hooks/emergencyKeys';
import { useQueryClient } from '@tanstack/react-query';
import { useEmergencyStore } from '@/store/emergencyStore';
import {
  customerSosHeadline,
  isActionableEmergencyRequest,
  isAssignedStatus,
  isSearchingStatus,
  isTerminalFailure,
  resolveEmergencyStatus,
  type EmergencyRequest,
} from '@/types/emergency';
import { AppError, getErrorMessage } from '@/utils/errors';

export function useEmergencySession() {
  const queryClient = useQueryClient();
  const request = useEmergencyStore((s) => s.request);
  const setRequest = useEmergencyStore((s) => s.setRequest);
  const setAssignedLawyer = useEmergencyStore((s) => s.setAssignedLawyer);
  const setCountdown = useEmergencyStore((s) => s.setCountdown);
  const setPhase = useEmergencyStore((s) => s.setPhase);
  const pushStatus = useEmergencyStore((s) => s.pushStatus);
  const setError = useEmergencyStore((s) => s.setError);
  const joinedRef = useRef<string | null>(null);

  const applyRequest = useCallback(
    async (next: EmergencyRequest) => {
      setRequest(next);
      queryClient.setQueryData(
        emergencyKeys.customerActive(),
        isActionableEmergencyRequest(next) ? next : null,
      );
      const status = resolveEmergencyStatus(next);

      if (isSearchingStatus(status)) {
        setPhase('searching');
        pushStatus(customerSosHeadline(next));
        return;
      }

      if (isAssignedStatus(status) && next.assignedLawyerId) {
        pushStatus(customerSosHeadline(next));
        setPhase(status === 'Calling' ? 'calling' : status === 'Active' || status === 'InProgress' ? 'video' : 'accepted');
        try {
          const lawyer = await lawyersApi.getById(next.assignedLawyerId);
          setAssignedLawyer(lawyer);
        } catch {
          setAssignedLawyer(null);
        }
        return;
      }

      if (status === 'Completed') {
        setPhase('rating');
        return;
      }

      if (status === 'Cancelled') {
        setError('lawyer_cancelled', next.cancellationReason || 'This emergency request was cancelled.');
        return;
      }

      if (isTerminalFailure(status)) {
        setError(
          status === 'Expired' ? 'timeout' : 'no_lawyers',
          next.cancellationReason || 'This emergency request is no longer active.',
        );
      }
    },
    [pushStatus, queryClient, setAssignedLawyer, setError, setPhase, setRequest],
  );

  useEffect(() => {
    let cancelled = false;
    let poll: ReturnType<typeof setInterval> | undefined;

    const unsubscribe = subscribeEmergencyHub({
      onStatusChanged: (payload) => {
        void queryClient.invalidateQueries({ queryKey: emergencyKeys.customerActive() });
        if (!cancelled && payload?.requestId) void applyRequest(payload);
      },
      onCountdown: (payload) => {
        if (cancelled) return;
        setCountdown(payload);
      },
    });

    const boot = async () => {
      if (!request?.requestId) return;
      try {
        await joinEmergencyRequest(request.requestId);
        joinedRef.current = request.requestId;
      } catch {
        // SignalR is not required to keep a valid SOS in Searching.
      }

      try {
        const fresh = await emergencyApi.getSos(request.requestId);
        if (!cancelled) await applyRequest(fresh);
      } catch {
        // Keep the last known successful create/search state.
      }

      poll = setInterval(() => {
        const id = useEmergencyStore.getState().request?.requestId;
        if (!id || cancelled) return;
        void emergencyApi
          .getSos(id)
          .then((fresh) => {
            if (!cancelled) void applyRequest(fresh);
          })
          .catch(() => undefined);
      }, 4000);
    };

    void boot();

    return () => {
      cancelled = true;
      unsubscribe();
      if (poll) clearInterval(poll);
      if (joinedRef.current) {
        void leaveEmergencyRequest(joinedRef.current);
        joinedRef.current = null;
      }
    };
  }, [applyRequest, queryClient, request?.requestId, setCountdown]);

  useEffect(() => {
    const onAppState = (state: AppStateStatus) => {
      const current = useEmergencyStore.getState().request;
      if (state !== 'active' || !current?.requestId) return;
      void ensureEmergencyHub().catch(() => undefined);
      void emergencyApi
        .getSos(current.requestId)
        .then((fresh) => applyRequest(fresh))
        .catch(() => undefined);
    };

    const sub = AppState.addEventListener('change', onAppState);
    return () => sub.remove();
  }, [applyRequest]);

  const restoreActiveSos = useCallback(async () => {
    try {
      const active = await emergencyApi.getActiveSos();
      if (!active) return null;
      await applyRequest(active);
      return active;
    } catch {
      return null;
    }
  }, [applyRequest]);

  const startSos = useCallback(
    async (input: { categoryId: string; latitude: number; longitude: number; notes?: string }) => {
      setPhase('searching');
      pushStatus('Starting emergency...');
      try {
        const created = await emergencyApi.createSos(input);
        await applyRequest(created);
        return created;
      } catch (error) {
        const conflict =
          error instanceof AppError &&
          (error.status === 409 || /already have an active emergency/i.test(error.message));
        if (conflict) {
          const active = await emergencyApi.getActiveSos().catch(() => null);
          if (active && (isSearchingStatus(resolveEmergencyStatus(active)) || isAssignedStatus(resolveEmergencyStatus(active)))) {
            await applyRequest(active);
            return active;
          }
        }
        setError('generic', getErrorMessage(error, 'Could not start emergency SOS.'));
        throw error;
      }
    },
    [applyRequest, pushStatus, setError, setPhase],
  );

  const cancelSos = useCallback(async (reason?: string) => {
    const id = useEmergencyStore.getState().request?.requestId;
    if (!id) return;
    await emergencyApi.cancelSos(id, reason);
    queryClient.setQueryData(emergencyKeys.customerActive(), null);
    void queryClient.invalidateQueries({ queryKey: emergencyKeys.customerActive() });
    useEmergencyStore.getState().resetSession();
  }, [queryClient]);

  const completeSos = useCallback(async () => {
    const id = useEmergencyStore.getState().request?.requestId;
    if (!id) return;
    const completed = await emergencyApi.completeSos(id);
    setRequest(completed);
    queryClient.setQueryData(emergencyKeys.customerActive(), null);
    void queryClient.invalidateQueries({ queryKey: emergencyKeys.customerActive() });
    setPhase('rating');
  }, [queryClient, setPhase, setRequest]);

  const teardown = useCallback(async () => {
    if (joinedRef.current) {
      await leaveEmergencyRequest(joinedRef.current);
      joinedRef.current = null;
    }
    await stopEmergencyHub();
    void queryClient.invalidateQueries({ queryKey: emergencyKeys.customerActive() });
    useEmergencyStore.getState().resetSession();
  }, [queryClient]);

  return {
    startSos,
    cancelSos,
    completeSos,
    teardown,
    applyRequest,
    restoreActiveSos,
  };
}
