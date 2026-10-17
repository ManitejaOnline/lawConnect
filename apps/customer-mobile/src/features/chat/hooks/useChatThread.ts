import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useInfiniteQuery, useQueryClient } from '@tanstack/react-query';
import * as DocumentPicker from 'expo-document-picker';
import * as ImagePicker from 'expo-image-picker';
import * as FileSystem from 'expo-file-system/legacy';

import { chatApi } from '@/services/api/chatApi';
import {
  ensureChatHub,
  joinConversation,
  leaveConversation,
  markDelivered,
  markReadViaHub,
  sendTyping,
} from '@/services/realtime/chatHub';
import { useAuthStore } from '@/store/authStore';
import { useChatStore } from '@/store/chatStore';
import type { ChatMessage, MessageCursorPage } from '@/types/chat';
import { getApiBaseUrl } from '@/config/apiBaseUrl';
import { getErrorMessage } from '@/utils/errors';
import { useToastStore } from '@/store/toastStore';
import { chatQueryKeys } from '@/features/chat/hooks/useConversations';

function sortAsc(items: ChatMessage[]) {
  return [...items].sort((a, b) => Date.parse(a.sentAtUtc) - Date.parse(b.sentAtUtc));
}

function mergeUnique(existing: ChatMessage[], incoming: ChatMessage[]) {
  const map = new Map<string, ChatMessage>();
  for (const m of [...existing, ...incoming]) {
    map.set(m.messageId, m);
  }
  return sortAsc([...map.values()]);
}

export function useChatThread(conversationId: string) {
  const queryClient = useQueryClient();
  const showToast = useToastStore((s) => s.show);
  const userId = useAuthStore((s) => s.session?.userId) ?? '';
  const setTyping = useChatStore((s) => s.setTyping);
  const patchConversation = useChatStore((s) => s.patchConversation);
  const applyIncomingMessagePreview = useChatStore((s) => s.applyIncomingMessagePreview);
  const typingUserId = useChatStore((s) => s.typingByConversation[conversationId] ?? null);
  const reactions = useChatStore((s) => s.reactions);
  const setReaction = useChatStore((s) => s.setReaction);

  const [localMessages, setLocalMessages] = useState<ChatMessage[]>([]);
  const [sending, setSending] = useState(false);
  const typingTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const lastReadId = useRef<string | null>(null);

  const messagesQuery = useInfiniteQuery({
    queryKey: chatQueryKeys.messages(conversationId),
    initialPageParam: null as string | null,
    queryFn: async ({ pageParam }) => chatApi.getMessages(conversationId, pageParam, 30),
    getNextPageParam: (last: MessageCursorPage) =>
      last.hasMore ? last.nextCursor ?? undefined : undefined,
  });

  const remoteMessages = useMemo(() => {
    const pages = messagesQuery.data?.pages ?? [];
    // API returns newest-first per page; flatten then sort ascending for chat UI
    const flat = pages.flatMap((p) => p.items);
    return sortAsc(flat);
  }, [messagesQuery.data?.pages]);

  const messages = useMemo(
    () => mergeUnique(remoteMessages, localMessages),
    [localMessages, remoteMessages],
  );

  const upsertMessage = useCallback((message: ChatMessage) => {
    setLocalMessages((prev) => mergeUnique(prev, [message]));
  }, []);

  const markLatestRead = useCallback(
    async (list: ChatMessage[]) => {
      const latestOther = [...list].reverse().find((m) => m.senderUserId !== userId);
      if (!latestOther || latestOther.messageId === lastReadId.current) return;
      lastReadId.current = latestOther.messageId;
      try {
        await markReadViaHub(conversationId, latestOther.messageId);
        await chatApi.markRead(conversationId, latestOther.messageId);
        patchConversation(conversationId, { unreadCount: 0 });
      } catch {
        // Non-blocking
      }
    },
    [conversationId, patchConversation, userId],
  );

  useEffect(() => {
    let cancelled = false;

    const boot = async () => {
      try {
        await ensureChatHub({
          onMessageReceived: (message) => {
            if (cancelled || message.conversationId !== conversationId) return;
            upsertMessage(message);
            applyIncomingMessagePreview(message, userId);
            if (message.senderUserId !== userId) {
              void markDelivered(message.messageId);
              void markLatestRead([message]);
            }
          },
          onTyping: (payload) => {
            if (cancelled || payload.conversationId !== conversationId) return;
            if (payload.userId === userId) return;
            setTyping(conversationId, payload.isTyping ? payload.userId : null);
          },
          onReadReceipt: (payload) => {
            if (cancelled || payload.conversationId !== conversationId) return;
            setLocalMessages((prev) =>
              prev.map((m) => {
                if (m.senderUserId !== userId) return m;
                const sent = Date.parse(m.sentAtUtc);
                const readMsg = prev.find((x) => x.messageId === payload.messageId);
                const readAt = readMsg ? Date.parse(readMsg.sentAtUtc) : Number.POSITIVE_INFINITY;
                if (sent <= readAt || m.messageId === payload.messageId) {
                  return { ...m, deliveryState: 'Read' };
                }
                return m;
              }),
            );
            void queryClient.invalidateQueries({ queryKey: chatQueryKeys.messages(conversationId) });
          },
        });
        await joinConversation(conversationId);
      } catch (error) {
        showToast({
          title: 'Chat connection issue',
          message: getErrorMessage(error),
          variant: 'warning',
        });
      }
    };

    void boot();

    return () => {
      cancelled = true;
      void leaveConversation(conversationId);
      if (typingTimer.current) clearTimeout(typingTimer.current);
      void sendTyping(conversationId, false);
    };
  }, [
    applyIncomingMessagePreview,
    conversationId,
    markLatestRead,
    queryClient,
    setTyping,
    showToast,
    upsertMessage,
    userId,
  ]);

  useEffect(() => {
    if (messages.length) void markLatestRead(messages);
  }, [markLatestRead, messages]);

  const notifyTyping = useCallback(
    (text: string) => {
      void sendTyping(conversationId, text.trim().length > 0);
      if (typingTimer.current) clearTimeout(typingTimer.current);
      typingTimer.current = setTimeout(() => {
        void sendTyping(conversationId, false);
      }, 1200);
    },
    [conversationId],
  );

  const sendText = useCallback(
    async (body: string) => {
      const trimmed = body.trim();
      if (!trimmed || sending) return;
      setSending(true);
      const optimisticId = `local-${Date.now()}`;
      const optimistic: ChatMessage = {
        messageId: optimisticId,
        conversationId,
        senderUserId: userId,
        body: trimmed,
        type: 'Text',
        sentAtUtc: new Date().toISOString(),
        attachments: [],
        deliveryState: 'Sent',
        pending: true,
      };
      upsertMessage(optimistic);
      try {
        const saved = await chatApi.sendMessage(conversationId, { body: trimmed, type: 0 });
        setLocalMessages((prev) =>
          mergeUnique(
            prev.filter((m) => m.messageId !== optimisticId),
            [saved],
          ),
        );
        applyIncomingMessagePreview(saved, userId);
        void sendTyping(conversationId, false);
      } catch (error) {
        setLocalMessages((prev) => prev.filter((m) => m.messageId !== optimisticId));
        showToast({
          title: 'Message not sent',
          message: getErrorMessage(error),
          variant: 'error',
        });
      } finally {
        setSending(false);
      }
    },
    [applyIncomingMessagePreview, conversationId, sending, showToast, upsertMessage, userId],
  );

  const uploadAndSend = useCallback(
    async (input: {
      uri: string;
      fileName: string;
      contentType: string;
      fileSizeBytes: number;
      type: 1 | 2;
    }) => {
      setSending(true);
      try {
        const signed = await chatApi.signUpload({
          conversationId,
          fileName: input.fileName,
          contentType: input.contentType,
          fileSizeBytes: input.fileSizeBytes,
        });

        const absolute = signed.uploadUrl.startsWith('http')
          ? signed.uploadUrl
          : `${getApiBaseUrl()}${signed.uploadUrl.startsWith('/') ? '' : '/'}${signed.uploadUrl}`;

        const token = useAuthStore.getState().session?.accessToken;
        const headers: Record<string, string> = { 'Content-Type': input.contentType };
        if (!signed.uploadUrl.startsWith('http') && token) {
          headers.Authorization = `Bearer ${token}`;
        }

        const uploadResult = await FileSystem.uploadAsync(absolute, input.uri, {
          httpMethod: 'PUT',
          uploadType: FileSystem.FileSystemUploadType.BINARY_CONTENT,
          headers,
        });

        if (uploadResult.status < 200 || uploadResult.status >= 300) {
          throw new Error(`Upload failed (${uploadResult.status})`);
        }

        const saved = await chatApi.sendMessage(conversationId, {
          body: null,
          type: input.type,
          attachmentStorageKey: signed.storageKey,
          attachmentFileName: input.fileName,
          attachmentContentType: input.contentType,
          attachmentFileSizeBytes: input.fileSizeBytes,
        });
        upsertMessage(saved);
        applyIncomingMessagePreview(saved, userId);
      } catch (error) {
        showToast({
          title: 'Upload failed',
          message: getErrorMessage(error),
          variant: 'error',
        });
      } finally {
        setSending(false);
      }
    },
    [applyIncomingMessagePreview, conversationId, showToast, upsertMessage, userId],
  );

  const pickImage = useCallback(async () => {
    const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!permission.granted) {
      showToast({
        title: 'Permission needed',
        message: 'Allow photo library access to send images.',
        variant: 'warning',
      });
      return;
    }
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ['images'],
      quality: 0.85,
      allowsMultipleSelection: false,
    });
    if (result.canceled || !result.assets[0]) return;
    const asset = result.assets[0];
    const fileName = asset.fileName || `photo-${Date.now()}.jpg`;
    const contentType = asset.mimeType || 'image/jpeg';
    const fileSizeBytes = asset.fileSize || 1;
    await uploadAndSend({
      uri: asset.uri,
      fileName,
      contentType,
      fileSizeBytes,
      type: 1,
    });
  }, [showToast, uploadAndSend]);

  const pickPdf = useCallback(async () => {
    const result = await DocumentPicker.getDocumentAsync({
      type: 'application/pdf',
      copyToCacheDirectory: true,
      multiple: false,
    });
    if (result.canceled || !result.assets[0]) return;
    const asset = result.assets[0];
    await uploadAndSend({
      uri: asset.uri,
      fileName: asset.name || `document-${Date.now()}.pdf`,
      contentType: asset.mimeType || 'application/pdf',
      fileSizeBytes: asset.size || 1,
      type: 2,
    });
  }, [uploadAndSend]);

  const loadOlder = useCallback(async () => {
    if (messagesQuery.hasNextPage && !messagesQuery.isFetchingNextPage) {
      await messagesQuery.fetchNextPage();
    }
  }, [messagesQuery]);

  return {
    messages,
    sending,
    isLoading: messagesQuery.isLoading,
    isError: messagesQuery.isError,
    refetch: messagesQuery.refetch,
    loadOlder,
    hasMore: Boolean(messagesQuery.hasNextPage),
    loadingOlder: messagesQuery.isFetchingNextPage,
    isOtherTyping: Boolean(typingUserId),
    sendText,
    notifyTyping,
    pickImage,
    pickPdf,
    reactions,
    setReaction,
    userId,
  };
}
