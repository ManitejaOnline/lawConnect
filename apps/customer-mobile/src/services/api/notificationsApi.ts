import { apiClient } from './client';
import type { NotificationItem, PagedResult } from '@/types/home';

export const notificationsApi = {
  list: async (pageNumber = 1, pageSize = 20, unreadOnly = false) => {
    const { data } = await apiClient.get<PagedResult<NotificationItem>>('/api/notifications', {
      params: { pageNumber, pageSize, unreadOnly },
    });
    return data;
  },

  markRead: async (notificationId: string) => {
    await apiClient.post(`/api/notifications/${notificationId}/read`);
  },
};
