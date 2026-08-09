import { useCallback } from 'react';

import { emergencyApi } from '@/services/api/emergencyApi';

/** Fetches the open customer SOS without mounting SignalR/error side effects. */
export function useRestoreActiveEmergency() {
  return useCallback(async () => {
    try {
      return await emergencyApi.getActiveSos();
    } catch {
      return null;
    }
  }, []);
}
