import { useCallback, useEffect, useMemo, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';

import { chatApi } from '@/services/api/chatApi';
import { ensureChatHub } from '@/services/realtime/chatHub';
import { useAuthStore } from '@/store/authStore';
import { useChatStore } from '@/store/chatStore';
import type { ChatMessage } from '@/types/chat';

export const chatQueryKeys = {
  all: ['chat'] as const,
  conversations: (page = 1) => [...chatQueryKeys.all, 'conversations', page] as const,
  messages: (conversationId: string) => [...chatQueryKeys.all, 'messages', conversationId] as const,
};

export function useConversationsQuery() {
  const setConversations = useChatStore((s) => s.setConversations);

  return useQuery({
    queryKey: chatQueryKeys.conversations(1),
    queryFn: async () => {
      const page = await chatApi.getConversations(1, 50);
      setConversations(page.items);
      return page;
    },
  });
}

/** Keeps chat hub alive for presence + inbox previews while on conversations list. */
export function useChatInboxRealtime() {
  const queryClient = useQueryClient();
  const userId = useAuthStore((s) => s.session?.userId);
  const applyIncomingMessagePreview = useChatStore((s) => s.applyIncomingMessagePreview);
  const setTyping = useChatStore((s) => s.setTyping);
  const setPresence = useChatStore((s) => s.setPresence);

  useEffect(() => {
    let cancelled = false;

    void ensureChatHub({
      onMessageReceived: (message: ChatMessage) => {
        if (cancelled || !userId) return;
        applyIncomingMessagePreview(message, userId);
        void queryClient.invalidateQueries({ queryKey: chatQueryKeys.messages(message.conversationId) });
      },
      onTyping: (payload) => {
        if (cancelled) return;
        if (payload.userId === userId) return;
        setTyping(payload.conversationId, payload.isTyping ? payload.userId : null);
      },
      onPresenceChanged: (payload) => {
        if (!cancelled) setPresence(payload);
      },
      onReadReceipt: () => {
        // List does not show per-message ticks; thread handles receipts.
      },
    });

    return () => {
      cancelled = true;
    };
  }, [applyIncomingMessagePreview, queryClient, setPresence, setTyping, userId]);
}

export function useFilteredConversations(search: string) {
  const conversations = useChatStore((s) => s.conversations);
  const query = useConversationsQuery();

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    const source = conversations.length ? conversations : query.data?.items ?? [];
    if (!q) return source;
    return source.filter((c) => {
      const name = `${c.otherFirstName} ${c.otherLastName}`.toLowerCase();
      const preview = (c.lastMessagePreview ?? '').toLowerCase();
      return name.includes(q) || preview.includes(q);
    });
  }, [conversations, query.data?.items, search]);

  const totalUnread = useMemo(
    () => (conversations.length ? conversations : query.data?.items ?? []).reduce((s, c) => s + c.unreadCount, 0),
    [conversations, query.data?.items],
  );

  return { filtered, totalUnread, query, refetch: query.refetch };
}

export function useConversationSearchState() {
  const [search, setSearch] = useState('');
  const clear = useCallback(() => setSearch(''), []);
  return { search, setSearch, clear };
}
