import { navigationRef } from '@/navigation/navigationRef';

export type EmergencyDeepLink =
  | { kind: 'incoming'; requestId?: string; assignmentId?: string }
  | { kind: 'active'; requestId?: string };

export function parseEmergencyDeepLink(url: string): EmergencyDeepLink | null {
  const trimmed = url.trim();
  if (!trimmed.startsWith('lawconnect://')) return null;
  const withoutScheme = trimmed.slice('lawconnect://'.length);
  const [pathPart, queryPart] = withoutScheme.split('?');
  const path = pathPart.replace(/^\/+/, '');
  const params = new URLSearchParams(queryPart ?? '');
  const requestId = params.get('requestId') ?? undefined;
  const assignmentId = params.get('assignmentId') ?? undefined;

  if (path.startsWith('emergency/incoming')) {
    return { kind: 'incoming', requestId, assignmentId };
  }
  if (path.startsWith('emergency/active')) {
    return { kind: 'active', requestId };
  }
  if (path.startsWith('emergency')) {
    return { kind: 'incoming', requestId, assignmentId };
  }
  return null;
}

export function parseEmergencyNotificationData(dataJson?: string | null): EmergencyDeepLink | null {
  if (!dataJson) return null;
  try {
    const data = JSON.parse(dataJson) as Record<string, string>;
    if (data.deepLink) {
      const fromLink = parseEmergencyDeepLink(data.deepLink);
      if (fromLink) return fromLink;
    }
    const type = data.type ?? data.screen ?? '';
    if (type.includes('incoming') || type.includes('LawyerEmergencyIncoming')) {
      return {
        kind: 'incoming',
        requestId: data.requestId,
        assignmentId: data.assignmentId,
      };
    }
    if (type.includes('active') || type.includes('LawyerEmergencyActive') || type.includes('emergency.status')) {
      return { kind: 'active', requestId: data.requestId };
    }
  } catch {
    return null;
  }
  return null;
}

export function openLawyerEmergency(target: EmergencyDeepLink) {
  if (!navigationRef.isReady()) return;
  if (target.kind === 'active' && target.requestId) {
    navigationRef.navigate('LawyerMain', {
      screen: 'LawyerEmergencyTab',
      params: { screen: 'LawyerEmergencyActive', params: { requestId: target.requestId } },
    });
    return;
  }
  navigationRef.navigate('LawyerMain', {
    screen: 'LawyerEmergencyTab',
    params: { screen: 'LawyerEmergencyInbox' },
  });
}
