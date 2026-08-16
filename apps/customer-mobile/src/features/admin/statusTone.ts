import type { VerificationStatus } from '@/types/api';

/**
 * Statuses meaning "awaiting an admin decision" — Pending is registered but
 * not yet submitted, UnderReview is a fully submitted application. The Admin
 * "Pending" queue and dashboard stat cover both, since a lawyer who actually
 * finishes registering (the realistic path) ends up UnderReview.
 */
export const AWAITING_DECISION_STATUSES: VerificationStatus[] = ['Pending', 'UnderReview'];

/** Whether an application is still awaiting an admin decision (see AWAITING_DECISION_STATUSES). */
export function isReviewable(status: VerificationStatus): boolean {
  return (AWAITING_DECISION_STATUSES as string[]).includes(status);
}

export function statusTone(status: VerificationStatus): 'success' | 'warning' | 'danger' | 'neutral' {
  switch (status) {
    case 'Approved':
      return 'success';
    case 'Rejected':
    case 'Suspended':
      return 'danger';
    case 'Pending':
    case 'UnderReview':
      return 'warning';
    default:
      return 'neutral';
  }
}
