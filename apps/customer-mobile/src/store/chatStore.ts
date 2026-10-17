import { create } from 'zustand';

import type { ChatMessage, ConversationListItem, PresenceChangedPayload } from '@/types/chat';

type ChatState = {
  conversations: ConversationListItem[];
  typingByConversation: Record<string, string | null>;
  presenceByUser: Record<string, PresenceChangedPayload>;
  reactions: Record<string, string>;
  setConversations: (items: ConversationListItem[]) => void;
  upsertConversation: (item: ConversationListItem) => void;
  patchConversation: (conversationId: string, patch: Partial<ConversationListItem>) => void;
  setTyping: (conversationId: string, userId: string | null) => void;
  setPresence: (payload: PresenceChangedPayload) => void;
  setReaction: (messageId: string, emoji: string | null) => void;
  applyIncomingMessagePreview: (message: ChatMessage, currentUserId: string) => void;
  totalUnread: () => number;
  reset: () => void;
};

const initialChatState = {
  conversations: [] as ConversationListItem[],
  typingByConversation: {} as Record<string, string | null>,
  presenceByUser: {} as Record<string, PresenceChangedPayload>,
  reactions: {} as Record<string, string>,
};

export const useChatStore = create<ChatState>((set, get) => ({
  ...initialChatState,
  reset: () => set(initialChatState),
  setConversations: (conversations) => set({ conversations }),
  upsertConversation: (item) =>
    set((state) => {
      const without = state.conversations.filter((c) => c.conversationId !== item.conversationId);
      return { conversations: [item, ...without] };
    }),
  patchConversation: (conversationId, patch) =>
    set((state) => ({
      conversations: state.conversations.map((c) =>
        c.conversationId === conversationId ? { ...c, ...patch } : c,
      ),
    })),
  setTyping: (conversationId, userId) =>
    set((state) => ({
      typingByConversation: { ...state.typingByConversation, [conversationId]: userId },
    })),
  setPresence: (payload) =>
    set((state) => ({
      presenceByUser: { ...state.presenceByUser, [payload.userId]: payload },
      conversations: state.conversations.map((c) =>
        c.otherUserId === payload.userId
          ? {
              ...c,
              otherIsOnline: payload.isOnline,
              otherLastSeenAtUtc: payload.lastSeenAtUtc ?? c.otherLastSeenAtUtc,
            }
          : c,
      ),
    })),
  setReaction: (messageId, emoji) =>
    set((state) => {
      const next = { ...state.reactions };
      if (!emoji) delete next[messageId];
      else next[messageId] = emoji;
      return { reactions: next };
    }),
  applyIncomingMessagePreview: (message, currentUserId) =>
    set((state) => {
      const preview =
        message.body?.trim() ||
        (String(message.type) === 'Image' || message.type === 1
          ? '📷 Photo'
          : String(message.type) === 'Pdf' || message.type === 2
            ? '📄 PDF'
            : 'New message');
      const isMine = message.senderUserId === currentUserId;
      return {
        conversations: state.conversations
          .map((c) => {
            if (c.conversationId !== message.conversationId) return c;
            return {
              ...c,
              lastMessagePreview: preview,
              lastMessageAtUtc: message.sentAtUtc,
              unreadCount: isMine ? c.unreadCount : c.unreadCount + 1,
            };
          })
          .sort((a, b) => {
            const at = a.lastMessageAtUtc ? Date.parse(a.lastMessageAtUtc) : 0;
            const bt = b.lastMessageAtUtc ? Date.parse(b.lastMessageAtUtc) : 0;
            return bt - at;
          }),
      };
    }),
  totalUnread: () => get().conversations.reduce((sum, c) => sum + (c.unreadCount || 0), 0),
}));
