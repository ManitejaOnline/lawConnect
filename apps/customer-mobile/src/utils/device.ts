import { Platform } from 'react-native';
import * as Crypto from 'expo-crypto';

import { useAuthStore } from '@/store/authStore';

export { getApiBaseUrl } from '@/config/apiBaseUrl';

/** Matches LawConnect.Domain.Enums.DevicePlatform. */
export type DevicePlatformCode = 0 | 1 | 2 | 3;

export function getDevicePlatform(): DevicePlatformCode {
  if (Platform.OS === 'ios') return 2;
  if (Platform.OS === 'android') return 1;
  if (Platform.OS === 'web') return 3;
  return 0;
}

/** @deprecated Prefer getDevicePlatform for API payloads. */
export function getPlatformName(): string {
  if (Platform.OS === 'ios') return 'Ios';
  if (Platform.OS === 'android') return 'Android';
  return 'Web';
}

export async function ensureDeviceIdentifier(): Promise<string> {
  const existing = useAuthStore.getState().deviceIdentifier;
  if (existing) {
    return existing;
  }
  const id = Crypto.randomUUID();
  useAuthStore.getState().setDeviceIdentifier(id);
  return id;
}
