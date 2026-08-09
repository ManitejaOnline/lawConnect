import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

import { useAuthStore } from '@/store/authStore';
import type {
  ChatMessage,
  PresenceChangedPayload,
  ReadReceiptPayload,
  TypingPayload,
} from '@/types/chat';
import { getApiBaseUrl } from '@/config/apiBaseUrl';

export type ChatHubHandlers = {
  onMessageReceived?: (message: ChatMessage) => void;
  onTyping?: (payload: TypingPayload) => void;
  onReadReceipt?: (payload: ReadReceiptPayload) => void;
  onPresenceChanged?: (payload: PresenceChangedPayload) => void;
};

let connection: HubConnection | null = null;
let handlers: ChatHubHandlers = {};

function buildConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${getApiBaseUrl()}/hubs/chat`, {
      accessTokenFactory: () => useAuthStore.getState().session?.accessToken ?? '',
    })
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
    .configureLogging(LogLevel.Warning)
    .build();
}

function bindHandlers(hub: HubConnection) {
  hub.off('MessageReceived');
  hub.off('Typing');
  hub.off('ReadReceipt');
  hub.off('PresenceChanged');

  hub.on('MessageReceived', (message: ChatMessage) => handlers.onMessageReceived?.(message));
  hub.on('Typing', (payload: TypingPayload) => handlers.onTyping?.(payload));
  hub.on('ReadReceipt', (payload: ReadReceiptPayload) => handlers.onReadReceipt?.(payload));
  hub.on('PresenceChanged', (payload: PresenceChangedPayload) =>
    handlers.onPresenceChanged?.(payload),
  );
}

export async function ensureChatHub(nextHandlers: ChatHubHandlers = {}): Promise<HubConnection> {
  handlers = { ...handlers, ...nextHandlers };

  if (!connection) {
    connection = buildConnection();
  }

  bindHandlers(connection);

  if (connection.state === HubConnectionState.Disconnected) {
    await connection.start();
  }

  return connection;
}

export async function joinConversation(conversationId: string): Promise<void> {
  const hub = await ensureChatHub();
  await hub.invoke('JoinConversation', conversationId);
}

export async function leaveConversation(conversationId: string): Promise<void> {
  if (!connection || connection.state !== HubConnectionState.Connected) return;
  try {
    await connection.invoke('LeaveConversation', conversationId);
  } catch {
    // Best-effort leave.
  }
}

export async function sendTyping(conversationId: string, isTyping: boolean): Promise<void> {
  if (!connection || connection.state !== HubConnectionState.Connected) return;
  try {
    await connection.invoke('Typing', conversationId, isTyping);
  } catch {
    // Ignore typing failures.
  }
}

export async function markDelivered(messageId: string): Promise<void> {
  if (!connection || connection.state !== HubConnectionState.Connected) return;
  try {
    await connection.invoke('MarkDelivered', messageId);
  } catch {
    // Best-effort.
  }
}

export async function markReadViaHub(conversationId: string, messageId: string): Promise<void> {
  if (!connection || connection.state !== HubConnectionState.Connected) return;
  try {
    await connection.invoke('MarkRead', conversationId, messageId);
  } catch {
    // Fall back to REST by caller if needed.
  }
}

export async function stopChatHub(): Promise<void> {
  if (!connection) return;
  try {
    await connection.stop();
  } finally {
    connection = null;
    handlers = {};
  }
}
