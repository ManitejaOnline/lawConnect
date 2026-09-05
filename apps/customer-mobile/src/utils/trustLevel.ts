import type { BadgeProps } from '@/components/ui/Badge';

export const NEW_LAWYER_TRUST_LEVEL = 'New Lawyer';

export function isProvisionalTrustLevel(level: string | null | undefined): boolean {
  const normalized = (level ?? '').trim();
  return normalized === NEW_LAWYER_TRUST_LEVEL || normalized === 'Not Yet Rated';
}

/** Maps a Trust Score level (from the backend's TrustScoreCalculator.TrustLevel bands) to a Badge tone. */
export function trustLevelTone(level: string): NonNullable<BadgeProps['tone']> {
  switch (level) {
    case 'Elite Trusted':
    case 'Highly Trusted':
      return 'success';
    case 'Trusted':
      return 'accent';
    case 'Established':
    case NEW_LAWYER_TRUST_LEVEL:
    case 'Not Yet Rated':
      return 'neutral';
    case 'Needs Improvement':
      return 'warning';
    default:
      return 'danger';
  }
}
