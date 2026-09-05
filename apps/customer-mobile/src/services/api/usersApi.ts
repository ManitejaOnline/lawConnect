import { apiClient } from './client';
import type { UserProfile } from '@/types/home';
import { appendPickedFile, type PickedFile } from '@/utils/uploadFile';

export const usersApi = {
  me: async () => {
    const { data } = await apiClient.get<UserProfile>('/api/users/me');
    return data;
  },

  update: async (payload: {
    firstName: string;
    lastName: string;
    email?: string | null;
    dateOfBirth?: string | null;
    gender?: string | null;
    preferredLanguage?: string | null;
  }) => {
    const { data } = await apiClient.put<UserProfile>('/api/users/me', payload);
    return data;
  },

  uploadProfilePicture: async (file: PickedFile) => {
    const form = new FormData();
    appendPickedFile(form, 'file', file);
    const { data } = await apiClient.post<UserProfile>('/api/users/me/profile-picture', form, {
      transformRequest: (body) => body,
    });
    return data;
  },
};
