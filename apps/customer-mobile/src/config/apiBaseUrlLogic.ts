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

/** Strip trailing slash. Used by tests; does not read process.env. */
export function normalizeConfiguredApiUrl(raw: string | undefined | null): string | null {
  const trimmed = raw?.trim() ?? '';
  return trimmed ? trimmed.replace(/\/$/, '') : null;
}

/**
 * Metro/Babel only inlines static `process.env.EXPO_PUBLIC_*` member access.
 * Do not pass process.env into a parameter and read env.EXPO_PUBLIC_API_URL.
 */
export function readEnvApiUrl(): string | null {
  return (
    normalizeConfiguredApiUrl(process.env.EXPO_PUBLIC_API_URL) ??
    normalizeConfiguredApiUrl(process.env.EXPO_PUBLIC_API_BASE_URL)
  );
}
