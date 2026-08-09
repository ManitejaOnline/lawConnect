import { apiClient } from './client';
import type {
  EmergencyCategory,
  EmergencyHistoryPage,
  EmergencyRequest,
  EmergencyTimelineItem,
  IncomingEmergencySos,
} from '@/types/emergency';

export const emergencyApi = {
  getCategories: async () => {
    const { data } = await apiClient.get<EmergencyCategory[]>('/api/emergency/categories');
    return data;
  },

  createSos: async (payload: {
    categoryId: string;
    latitude: number;
    longitude: number;
    notes?: string | null;
  }) => {
    const { data } = await apiClient.post<EmergencyRequest>('/api/emergency/sos', payload);
    return data;
  },

  getSos: async (requestId: string) => {
    const { data } = await apiClient.get<EmergencyRequest>(`/api/emergency/sos/${requestId}`);
    return data;
  },

  getActiveSos: async () => {
    const { data, status } = await apiClient.get<EmergencyRequest | ''>('/api/emergency/sos/active', {
      validateStatus: (code) => code === 200 || code === 204,
    });
    return status === 204 || !data ? null : data;
  },

  getLawyerActiveSos: async () => {
    const { data, status } = await apiClient.get<EmergencyRequest | ''>('/api/emergency/lawyer/active', {
      validateStatus: (code) => code === 200 || code === 204,
    });
    return status === 204 || !data ? null : data;
  },

  getTimeline: async (requestId: string) => {
    const { data } = await apiClient.get<EmergencyTimelineItem[]>(
      `/api/emergency/sos/${requestId}/timeline`,
    );
    return data;
  },

  cancelSos: async (requestId: string, reason?: string) => {
    await apiClient.post(`/api/emergency/sos/${requestId}/cancel`, { reason: reason ?? null });
  },

  completeSos: async (requestId: string) => {
    const { data } = await apiClient.post<EmergencyRequest>(`/api/emergency/sos/${requestId}/complete`);
    return data;
  },

  getHistory: async (pageNumber = 1, pageSize = 20) => {
    const { data } = await apiClient.get<EmergencyHistoryPage>('/api/emergency/history', {
      params: { pageNumber, pageSize },
    });
    return data;
  },

  getIncoming: async () => {
    const { data } = await apiClient.get<IncomingEmergencySos[]>('/api/emergency/incoming');
    return data;
  },

  acceptAssignment: async (assignmentId: string) => {
    const { data } = await apiClient.post<EmergencyRequest>(
      `/api/emergency/assignments/${assignmentId}/accept`,
    );
    return data;
  },

  declineAssignment: async (assignmentId: string) => {
    await apiClient.post(`/api/emergency/assignments/${assignmentId}/decline`);
  },
};
