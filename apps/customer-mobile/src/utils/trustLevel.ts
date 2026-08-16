import type { BadgeProps } from '@/components/ui/Badge';

/** Maps a Trust Score level (from the backend's TrustScoreCalculator.TrustLevel bands) to a Badge tone. */
export function trustLevelTone(level: string): NonNullable<BadgeProps['tone']> {
  switch (level) {
    case 'Elite Trusted':
    case 'Highly Trusted':
      return 'success';
    case 'Trusted':
      return 'accent';
    case 'Established':
      return 'neutral';
    case 'Needs Improvement':
      return 'warning';
    default:
      return 'danger';
  }
}
