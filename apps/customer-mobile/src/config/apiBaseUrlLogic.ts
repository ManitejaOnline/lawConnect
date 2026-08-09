export const API_DEVICE_NOT_CONFIGURED = 'API server is not configured for this device.';

/** Expo Go / Metro bundler addresses must never be used as the LawConnect API. */
export function isExpoBundlerUrl(value: string): boolean {
  const trimmed = value.trim();
  if (!trimmed) return false;
  if (/^exp[a-z+]*:/i.test(trimmed)) return true;
  if (/exp\.direct/i.test(trimmed)) return true;

  try {
    const withProtocol = trimmed.includes('://') ? trimmed : `http://${trimmed}`;
    const url = new URL(withProtocol);
    if (url.port === '8081') return true;
    if (url.protocol === 'exp:' || url.protocol.startsWith('exp+')) return true;
  } catch {
    return /:8081(\/|$)/.test(trimmed);
  }

  return false;
}

export function isLoopbackApiUrl(value: string): boolean {
  try {
    const hostname = new URL(value).hostname.toLowerCase();
    return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '[::1]' || hostname === '::1';
  } catch {
    return /^(https?:\/\/)?(localhost|127\.0\.0\.1|\[::1\])\b/i.test(value);
  }
}

export function readEnvApiUrl(env: Record<string, string | undefined> = process.env): string | null {
  const raw = env.EXPO_PUBLIC_API_URL?.trim() || env.EXPO_PUBLIC_API_BASE_URL?.trim() || '';
  return raw ? raw.replace(/\/$/, '') : null;
}
