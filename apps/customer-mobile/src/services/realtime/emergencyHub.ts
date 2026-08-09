import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

import { useAuthStore } from '@/store/authStore';
import type {
  EmergencyRequest,
  IncomingEmergencySos,
  SosAssignmentCancelledPayload,
  SosCountdownPayload,
} from '@/types/emergency';
import { getApiBaseUrl } from '@/config/apiBaseUrl';

export type EmergencyHubHandlers = {
  onStatusChanged?: (request: EmergencyRequest) => void;
  onCountdown?: (payload: SosCountdownPayload) => void;
  onIncomingSos?: (item: IncomingEmergencySos) => void;
  onAssignmentCancelled?: (payload: SosAssignmentCancelledPayload) => void;
};

let connection: HubConnection | null = null;
const statusListeners = new Set<(request: EmergencyRequest) => void>();
const countdownListeners = new Set<(payload: SosCountdownPayload) => void>();
const incomingListeners = new Set<(item: IncomingEmergencySos) => void>();
const cancelledListeners = new Set<(payload: SosAssignmentCancelledPayload) => void>();
const joinedRequestIds = new Set<string>();

function buildConnection(): HubConnection {
  const hub = new HubConnectionBuilder()
    .withUrl(`${getApiBaseUrl()}/hubs/emergency`, {
      accessTokenFactory: () => useAuthStore.getState().session?.accessToken ?? '',
    })
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
    .configureLogging(LogLevel.Warning)
    .build();

  hub.onreconnected(() => {
    bindHandlers(hub);
    void rejoinRequestGroups(hub);
  });

  return hub;
}

function emitStatus(request: EmergencyRequest): void {
  statusListeners.forEach((fn) => fn(request));
}

function bindHandlers(hub: HubConnection): void {
  hub.off('SosStatusChanged');
  hub.off('SosCountdown');
  hub.off('IncomingSos');
  hub.off('SosAssignmentCancelled');
  hub.off('EmergencyAssigned');
  hub.off('EmergencyCancelled');
  hub.off('EmergencyCompleted');

  hub.on('SosStatusChanged', emitStatus);
  hub.on('EmergencyAssigned', emitStatus);
  hub.on('EmergencyCancelled', emitStatus);
  hub.on('EmergencyCompleted', emitStatus);
  hub.on('SosCountdown', (payload: SosCountdownPayload) => {
    countdownListeners.forEach((fn) => fn(payload));
  });
  hub.on('IncomingSos', (item: IncomingEmergencySos) => {
    incomingListeners.forEach((fn) => fn(item));
  });
  hub.on('SosAssignmentCancelled', (payload: SosAssignmentCancelledPayload) => {
    cancelledListeners.forEach((fn) => fn(payload));
  });
}

async function rejoinRequestGroups(hub: HubConnection): Promise<void> {
  if (hub.state !== HubConnectionState.Connected) return;
  for (const requestId of joinedRequestIds) {
    try {
      await hub.invoke('JoinRequest', requestId);
    } catch {
      // Ownership can expire after cancel/complete; ignore and retry on next join.
    }
  }
}

export async function ensureEmergencyHub(): Promise<HubConnection> {
  if (!connection) {
    connection = buildConnection();
  }

  bindHandlers(connection);

  if (connection.state === HubConnectionState.Disconnected) {
    await connection.start();
    await rejoinRequestGroups(connection);
  }

  return connection;
}

/** Subscribe without replacing other listeners. Call the return value on unmount. */
export function subscribeEmergencyHub(handlers: EmergencyHubHandlers): () => void {
  if (handlers.onStatusChanged) statusListeners.add(handlers.onStatusChanged);
  if (handlers.onCountdown) countdownListeners.add(handlers.onCountdown);
  if (handlers.onIncomingSos) incomingListeners.add(handlers.onIncomingSos);
  if (handlers.onAssignmentCancelled) cancelledListeners.add(handlers.onAssignmentCancelled);

  void ensureEmergencyHub();

  return () => {
    if (handlers.onStatusChanged) statusListeners.delete(handlers.onStatusChanged);
    if (handlers.onCountdown) countdownListeners.delete(handlers.onCountdown);
    if (handlers.onIncomingSos) incomingListeners.delete(handlers.onIncomingSos);
    if (handlers.onAssignmentCancelled) cancelledListeners.delete(handlers.onAssignmentCancelled);
  };
}

export async function joinEmergencyRequest(requestId: string): Promise<void> {
  joinedRequestIds.add(requestId);
  const hub = await ensureEmergencyHub();
  if (hub.state === HubConnectionState.Connected) {
    await hub.invoke('JoinRequest', requestId);
  }
}

export async function leaveEmergencyRequest(requestId: string): Promise<void> {
  joinedRequestIds.delete(requestId);
  if (!connection || connection.state !== HubConnectionState.Connected) return;
  try {
    await connection.invoke('LeaveRequest', requestId);
  } catch {
    // Best-effort leave.
  }
}

export async function stopEmergencyHub(): Promise<void> {
  joinedRequestIds.clear();
  if (!connection) return;
  try {
    await connection.stop();
  } finally {
    connection = null;
  }
}
