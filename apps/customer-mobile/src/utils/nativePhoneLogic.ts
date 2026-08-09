export const PHONE_UNAVAILABLE = 'Phone number unavailable';
export const PHONE_APP_UNAVAILABLE = 'Unable to open the phone app.';

/**
 * Strips spaces, hyphens, and parentheses.
 * Keeps or adds + for international numbers (11–15 digits).
 */
export function normalizeDialablePhone(raw: string | null | undefined): string | null {
  if (!raw?.trim()) return null;
  const trimmed = raw.trim();
  const hadPlus = trimmed.startsWith('+');
  const digits = trimmed.replace(/\D/g, '');
  if (digits.length < 8 || digits.length > 15) return null;
  if (hadPlus || digits.length >= 11) {
    return `+${digits}`;
  }
  return digits;
}

export function toTelUri(phone: string): string {
  return `tel:${phone}`;
}
