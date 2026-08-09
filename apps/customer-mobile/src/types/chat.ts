import type { PagedResult } from '@/types/home';

export type ChatMessageType = 'Text' | 'Image' | 'Pdf' | 0 | 1 | 2;

export type ChatDeliveryState = 'Sent' | 'Delivered' | 'Read';

export type ChatAttachment = {
  attachmentId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  downloadUrl?: string | null;
};

export type ChatMessage = {
  messageId: string;
  conversationId: string;
  senderUserId: string;
  body?: string | null;
  type: ChatMessageType;
  sentAtUtc: string;
  attachments: ChatAttachment[];
  deliveryState: ChatDeliveryState;
  /** Client-only optimistic flag */
  pending?: boolean;
  /** Client-only local reaction emoji (UI only, not persisted) */
  localReaction?: string | null;
};

export type ConversationListItem = {
  conversationId: string;
  otherUserId: string;
  otherFirstName: string;
  otherLastName: string;
  otherProfileImageUrl?: string | null;
  otherIsOnline: boolean;
  otherLastSeenAtUtc?: string | null;
  lastMessagePreview?: string | null;
  lastMessageAtUtc?: string | null;
  unreadCount: number;
};

export type MessageCursorPage = {
  items: ChatMessage[];
  nextCursor?: string | null;
  hasMore: boolean;
};

export type ChatUploadUrlResponse = {
  storageKey: string;
  uploadUrl: string;
  expiresAtUtc: string;
};

export type SendMessagePayload = {
  body?: string | null;
  type: 0 | 1 | 2;
  attachmentStorageKey?: string | null;
  attachmentFileName?: string | null;
  attachmentContentType?: string | null;
  attachmentFileSizeBytes?: number | null;
};

export type TypingPayload = {
  conversationId: string;
  userId: string;
  isTyping: boolean;
};

export type ReadReceiptPayload = {
  conversationId: string;
  messageId: string;
  userId: string;
  readAtUtc: string;
};

export type PresenceChangedPayload = {
  userId: string;
  isOnline: boolean;
  lastSeenAtUtc?: string | null;
  connectionCount: number;
};

export type ConversationsPage = PagedResult<ConversationListItem>;

export const CHAT_REACTION_EMOJIS = ['👍', '❤️', '😂', '😮', '🙏', '✅'] as const;

export function normalizeMessageType(type: ChatMessageType): 'Text' | 'Image' | 'Pdf' {
  if (type === 0 || type === 'Text') return 'Text';
  if (type === 1 || type === 'Image') return 'Image';
  return 'Pdf';
}

export function conversationDisplayName(item: ConversationListItem): string {
  return `${item.otherFirstName} ${item.otherLastName}`.trim() || 'Conversation';
}

export function formatChatTime(iso?: string | null): string {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const now = new Date();
  const sameDay =
    date.getFullYear() === now.getFullYear() &&
    date.getMonth() === now.getMonth() &&
    date.getDate() === now.getDate();
  if (sameDay) {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
  const yesterday = new Date(now);
  yesterday.setDate(now.getDate() - 1);
  if (
    date.getFullYear() === yesterday.getFullYear() &&
    date.getMonth() === yesterday.getMonth() &&
    date.getDate() === yesterday.getDate()
  ) {
    return 'Yesterday';
  }
  return date.toLocaleDateString([], { month: 'short', day: 'numeric' });
}
