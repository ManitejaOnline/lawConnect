import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

import { getApiBaseUrl } from '@/config/apiBaseUrl';
import { useAuthStore } from '@/store/authStore';

export type DocumentTransferNotice = {
  sessionId: string;
  documentId: string;
  fileName: string;
};

type Handlers = {
  onRequested?: (notice: DocumentTransferNotice) => void;
  onReady?: (notice: DocumentTransferNotice) => void;
};

let connection: HubConnection | null = null;
let handlers: Handlers = {};

function build() {
  const hub = new HubConnectionBuilder()
    .withUrl(`${getApiBaseUrl()}/hubs/documents`, {
      accessTokenFactory: () => useAuthStore.getState().session?.accessToken ?? '',
    })
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
    .configureLogging(LogLevel.Warning)
    .build();
  hub.on('TransferRequested', (notice: DocumentTransferNotice) => handlers.onRequested?.(notice));
  hub.on('TransferReady', (notice: DocumentTransferNotice) => handlers.onReady?.(notice));
  return hub;
}

export async function startDocumentHub(next: Handlers = {}) {
  handlers = { ...handlers, ...next };
  if (!connection) connection = build();
  if (connection.state === HubConnectionState.Disconnected) {
    await connection.start();
  }
}

export function setDocumentHubHandlers(next: Handlers) {
  handlers = { ...handlers, ...next };
}

export async function stopDocumentHub() {
  if (!connection) return;
  try {
    await connection.stop();
  } finally {
    connection = null;
    handlers = {};
  }
}
