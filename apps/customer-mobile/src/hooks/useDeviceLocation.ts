import { useCallback, useEffect, useState } from 'react';
import * as Location from 'expo-location';

export type DeviceLocation = {
  latitude: number;
  longitude: number;
  accuracy?: number | null;
  source: 'device' | 'fallback';
};

/** New Delhi — used when permission is denied or unavailable. */
export const FALLBACK_LOCATION: DeviceLocation = {
  latitude: 28.6139,
  longitude: 77.209,
  source: 'fallback',
};

export function useDeviceLocation() {
  const [location, setLocation] = useState<DeviceLocation>(FALLBACK_LOCATION);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const permission = await Location.requestForegroundPermissionsAsync();
      if (permission.status !== 'granted') {
        setLocation(FALLBACK_LOCATION);
        setError('Location permission denied — showing nearby from a default city.');
        return FALLBACK_LOCATION;
      }

      const current = await Location.getCurrentPositionAsync({
        accuracy: Location.Accuracy.Balanced,
      });

      const next: DeviceLocation = {
        latitude: current.coords.latitude,
        longitude: current.coords.longitude,
        accuracy: current.coords.accuracy,
        source: 'device',
      };
      setLocation(next);
      return next;
    } catch {
      setLocation(FALLBACK_LOCATION);
      setError('Could not read location — using default city.');
      return FALLBACK_LOCATION;
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return { location, loading, error, refresh };
}
