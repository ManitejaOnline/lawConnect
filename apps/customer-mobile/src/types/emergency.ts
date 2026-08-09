import type { PagedResult } from '@/types/home';

/** Mirrors LawConnect.Domain.Enums.EmergencyStatus (numeric or named). */
export type EmergencyStatusCode =
  | 0
  | 1
  | 2
  | 3
  | 4
  | 5
  | 6
  | 7
  | 8
  | 9
  | 10
  | 'Searching'
  | 'Notifying'
  | 'Assigned'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'Expired'
  | 'Failed'
  | 'Created'
  | 'Calling'
  | 'Active';

export type EmergencyCategory = {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  specializationId: string;
  specializationName: string;
  displayOrder: number;
};

export type EmergencyRequest = {
  requestId: string;
  customerId: string;
  customerUserId: string;
  categoryId: string;
  categoryName: string;
  status: EmergencyStatusCode;
  statusName: string;
  latitude: number;
  longitude: number;
  attemptNumber: number;
  currentRadiusKm: number;
  cityName?: string | null;
  stateName?: string | null;
  countryName?: string | null;
  searchScope?: string | null;
  searchScopeLabel?: string | null;
  searchExpiresAtUtc?: string | null;
  remainingSeconds?: number | null;
  assignedLawyerId?: string | null;
  acceptedAssignmentId?: string | null;
  cancellationReason?: string | null;
  notes?: string | null;
  createdAtUtc: string;
  assignedAtUtc?: string | null;
  completedAtUtc?: string | null;
};

export type EmergencyTimelineItem = {
  id: string;
  eventType: string;
  message: string;
  actorUserId?: string | null;
  metadataJson?: string | null;
  occurredAtUtc: string;
};

export type EmergencyHistoryItem = {
  historyId: string;
  requestId: string;
  categoryId: string;
  lawyerId?: string | null;
  finalStatus: string;
  attemptCount: number;
  acceptanceTimeSeconds?: number | null;
  responseTimeSeconds?: number | null;
  cancellationReason?: string | null;
  closedAtUtc: string;
};

export type IncomingEmergencySos = {
  requestId: string;
  assignmentId: string;
  categoryId: string;
  categoryName: string;
  latitude: number;
  longitude: number;
  distanceKm: number;
  rank: number;
  attemptNumber: number;
  expiresAtUtc: string;
  remainingSeconds: number;
  notifiedAtUtc?: string;
  cityName?: string | null;
};

export type SosAssignmentCancelledPayload = {
  requestId: string;
  assignmentId: string;
  reason?: string | null;
};

export type SosCountdownPayload = {
  requestId: string;
  attemptNumber: number;
  expiresAtUtc: string;
  remainingSeconds: number;
  responseWindowSeconds?: number;
};

export type EmergencyUiCategory = {
  key: string;
  title: string;
  subtitle: string;
  /** Maps to seeded backend EmergencyCategories id when available. */
  backendCategoryId: string;
  icon: 'shield' | 'alert' | 'user' | 'home' | 'phone' | 'filter' | 'chat' | 'bell';
};

export type EmergencySessionPhase =
  | 'idle'
  | 'location'
  | 'category'
  | 'searching'
  | 'accepted'
  | 'calling'
  | 'video'
  | 'rating'
  | 'error';

export type EmergencyErrorKind =
  | 'location'
  | 'no_lawyers'
  | 'timeout'
  | 'network'
  | 'lawyer_cancelled'
  | 'generic';

export type EmergencyHistoryPage = PagedResult<EmergencyHistoryItem>;

export function normalizeEmergencyStatus(status: EmergencyStatusCode | string | number): string {
  if (typeof status === 'number') {
    const map = [
      'Searching',
      'Notifying',
      'Assigned',
      'InProgress',
      'Completed',
      'Cancelled',
      'Expired',
      'Failed',
      'Created',
      'Calling',
      'Active',
    ];
    return map[status] ?? 'Searching';
  }
  return String(status);
}

export function resolveEmergencyStatus(request: {
  status: EmergencyStatusCode | string | number;
  statusName?: string | null;
}): string {
  const named = request.statusName?.trim();
  if (named && /^[A-Za-z]+$/.test(named)) {
    return named;
  }
  return normalizeEmergencyStatus(request.status);
}

export function isSearchingStatus(status: EmergencyStatusCode | string | number): boolean {
  const name = normalizeEmergencyStatus(status);
  return name === 'Created' || name === 'Searching' || name === 'Notifying';
}

export function isAssignedStatus(status: EmergencyStatusCode | string | number): boolean {
  const name = normalizeEmergencyStatus(status);
  return name === 'Assigned' || name === 'InProgress' || name === 'Calling' || name === 'Active';
}

/** Customer or lawyer still needs to act on / watch this SOS. */
export function isActionableEmergencyRequest(
  request: { status: EmergencyStatusCode | string | number; statusName?: string | null } | null | undefined,
): boolean {
  if (!request) return false;
  const name = resolveEmergencyStatus(request);
  return isSearchingStatus(name) || isAssignedStatus(name);
}

export function isIncomingAssignmentActionable(item: {
  expiresAtUtc?: string;
  remainingSeconds?: number;
}): boolean {
  if (item.expiresAtUtc) {
    const expires = new Date(item.expiresAtUtc).getTime();
    if (!Number.isNaN(expires)) return expires > Date.now();
  }
  return (item.remainingSeconds ?? 0) > 0;
}

export function isTerminalFailure(status: EmergencyStatusCode | string | number): boolean {
  const name = normalizeEmergencyStatus(status);
  return name === 'Expired' || name === 'Failed';
}

export function searchingRoundCopy(request: {
  attemptNumber?: number;
  searchScope?: string | null;
  cityName?: string | null;
  stateName?: string | null;
  countryName?: string | null;
}): string {
  const attempt = request.attemptNumber ?? 1;
  const scope = (request.searchScope ?? '').toLowerCase();
  if (attempt >= 2 || scope === 'state' || scope === 'country' || scope === 'international') {
    return 'Still looking for an available lawyer...';
  }
  return 'Searching for an available lawyer...';
}

export function customerSosHeadline(request: {
  status: EmergencyStatusCode | string | number;
  statusName?: string | null;
  attemptNumber?: number;
  searchScope?: string | null;
  cityName?: string | null;
  stateName?: string | null;
  countryName?: string | null;
}): string {
  const name = resolveEmergencyStatus(request);
  if (name === 'Created') return 'Searching for an available lawyer...';
  if (name === 'Assigned') return 'Lawyer found';
  if (name === 'Calling') return 'Connecting to lawyer...';
  if (name === 'Active' || name === 'InProgress') return 'Consultation active';
  if (name === 'Cancelled') return 'Emergency cancelled';
  if (name === 'Completed') return 'Emergency completed';
  if (name === 'Searching' || name === 'Notifying') {
    return searchingRoundCopy(request);
  }
  return 'Searching for an available lawyer...';
}
