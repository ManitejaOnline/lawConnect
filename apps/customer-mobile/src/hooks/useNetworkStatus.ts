import { useEffect, useState } from 'react';
import * as Network from 'expo-network';

export type NetworkStatus = {
  isConnected: boolean;
  isInternetReachable: boolean | null;
  type: string | null;
};

const defaultStatus: NetworkStatus = {
  isConnected: true,
  isInternetReachable: true,
  type: null,
};

/**
 * Lightweight offline detection via Expo Network polling.
 */
export function useNetworkStatus(pollMs = 4000): NetworkStatus {
  const [status, setStatus] = useState<NetworkStatus>(defaultStatus);

  useEffect(() => {
    let cancelled = false;

    const read = async () => {
      try {
        const state = await Network.getNetworkStateAsync();
        if (cancelled) return;
        setStatus({
          isConnected: Boolean(state.isConnected),
          isInternetReachable: state.isInternetReachable ?? null,
          type: state.type ?? null,
        });
      } catch {
        if (!cancelled) {
          setStatus(defaultStatus);
        }
      }
    };

    void read();
    const id = setInterval(() => {
      void read();
    }, pollMs);

    return () => {
      cancelled = true;
      clearInterval(id);
    };
  }, [pollMs]);

  return status;
}

/** Device has no network link. Reachability/API failures are not treated as offline. */
export function useIsOffline(): boolean {
  const status = useNetworkStatus();
  return status.isConnected === false;
}
