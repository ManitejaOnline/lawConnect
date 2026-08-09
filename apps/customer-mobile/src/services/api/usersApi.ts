import { apiClient } from './client';
import type { UserProfile } from '@/types/home';

export const usersApi = {
  me: async () => {
    const { data } = await apiClient.get<UserProfile>('/api/users/me');
    return data;
  },

  uploadProfilePicture: async (file: { uri: string; name: string; mimeType: string }) => {
    const form = new FormData();
    form.append('file', {
      uri: file.uri,
      name: file.name,
      type: file.mimeType,
    } as unknown as Blob);
    const { data } = await apiClient.post<UserProfile>('/api/users/me/profile-picture', form, {
      transformRequest: (body) => body,
    });
    return data;
  },
};
