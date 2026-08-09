const E164_REGEX = /^\+[1-9]\d{7,14}$/;

/**
 * Normalizes common local/international inputs to E.164 where possible.
 * Defaults bare 10-digit Indian mobiles to +91.
 */
export function normalizePhoneToE164(input: string): string {
  const trimmed = input.trim().replace(/[\s()-]/g, '');
  if (!trimmed) return '';

  if (trimmed.startsWith('+')) {
    return `+${trimmed.slice(1).replace(/\D/g, '')}`;
  }

  const digits = trimmed.replace(/\D/g, '');
  if (digits.length === 10) {
    return `+91${digits}`;
  }
  if (digits.length === 12 && digits.startsWith('91')) {
    return `+${digits}`;
  }
  if (digits.length >= 8 && digits.length <= 15) {
    return `+${digits}`;
  }
  return trimmed.startsWith('+') ? trimmed : `+${digits}`;
}

export function isValidE164(phone: string): boolean {
  return E164_REGEX.test(phone.trim());
}

export function maskPhone(phone: string): string {
  const normalized = normalizePhoneToE164(phone);
  if (normalized.length < 6) return normalized;
  const start = normalized.slice(0, 3);
  const end = normalized.slice(-2);
  return `${start}${'•'.repeat(Math.max(normalized.length - 5, 3))}${end}`;
}
