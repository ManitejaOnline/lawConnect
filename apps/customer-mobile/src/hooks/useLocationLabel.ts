import { useEffect, useState } from 'react';
import * as Location from 'expo-location';

import type { DeviceLocation } from '@/hooks/useDeviceLocation';

export function useLocationLabel(location?: DeviceLocation | null) {
  const [label, setLabel] = useState('Locating…');

  useEffect(() => {
    if (!location) {
      setLabel('Set location');
      return;
    }

    let cancelled = false;
    void Location.reverseGeocodeAsync({
      latitude: location.latitude,
      longitude: location.longitude,
    })
      .then((results) => {
        if (cancelled) return;
        const place = results[0];
        if (!place) {
          setLabel(location.source === 'fallback' ? 'Default city' : 'Current location');
          return;
        }
        const city = place.city || place.subregion || place.district || place.name;
        const region = place.region;
        const next = [city, region].filter(Boolean).join(', ');
        setLabel(next || (location.source === 'fallback' ? 'Default city' : 'Current location'));
      })
      .catch(() => {
        if (!cancelled) {
          setLabel(location.source === 'fallback' ? 'Default city' : 'Current location');
        }
      });

    return () => {
      cancelled = true;
    };
  }, [location?.latitude, location?.longitude, location?.source]);

  return label;
}
