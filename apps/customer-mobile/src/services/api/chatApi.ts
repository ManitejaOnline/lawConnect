import { apiClient } from './client';
import type {
  ChatMessage,
  ChatUploadUrlResponse,
  ConversationListItem,
  ConversationsPage,
  MessageCursorPage,
  SendMessagePayload,
} from '@/types/chat';
import { getApiBaseUrl } from '@/config/apiBaseUrl';
import { useAuthStore } from '@/store/authStore';

export const chatApi = {
  getConversations: async (pageNumber = 1, pageSize = 30) => {
    const { data } = await apiClient.get<ConversationsPage>('/api/chat/conversations', {
      params: { pageNumber, pageSize },
    });
    return data;
  },

  openConversation: async (otherUserId: string) => {
    const { data } = await apiClient.post<ConversationListItem>('/api/chat/conversations', {
      otherUserId,
    });
    return data;
  },

  getMessages: async (conversationId: string, cursor?: string | null, pageSize = 30) => {
    const { data } = await apiClient.get<MessageCursorPage>(
      `/api/chat/conversations/${conversationId}/messages`,
      { params: { cursor: cursor || undefined, pageSize } },
    );
    return data;
  },

  sendMessage: async (conversationId: string, payload: SendMessagePayload) => {
    const { data } = await apiClient.post<ChatMessage>(
      `/api/chat/conversations/${conversationId}/messages`,
      payload,
    );
    return data;
  },

  markRead: async (conversationId: string, messageId: string) => {
    await apiClient.post(`/api/chat/conversations/${conversationId}/read`, { messageId });
  },

  deleteMessage: async (messageId: string) => {
    await apiClient.delete(`/api/chat/messages/${messageId}`);
  },

  signUpload: async (input: {
    conversationId: string;
    fileName: string;
    contentType: string;
    fileSizeBytes: number;
  }) => {
    const { data } = await apiClient.post<ChatUploadUrlResponse>('/api/chat/uploads/sign', input);
    return data;
  },

  /** PUT file bytes to signed URL (absolute S3 or relative local storage). */
  uploadBytes: async (uploadUrl: string, bytes: ArrayBuffer, contentType: string) => {
    const absolute = uploadUrl.startsWith('http')
      ? uploadUrl
      : `${getApiBaseUrl()}${uploadUrl.startsWith('/') ? '' : '/'}${uploadUrl}`;

    const token = useAuthStore.getState().session?.accessToken;
    const headers: Record<string, string> = {
      'Content-Type': contentType,
    };
    if (!uploadUrl.startsWith('http') && token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(absolute, {
      method: 'PUT',
      headers,
      body: bytes,
    });

    if (!response.ok) {
      throw new Error(`Upload failed (${response.status})`);
    }
  },
};
