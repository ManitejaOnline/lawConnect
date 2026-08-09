import { apiClient } from './client';

export type CallContact = {
  userId: string;
  displayName: string;
  phoneNumber: string;
};

export const contactsApi = {
  getCallContact: async (userId: string) => {
    const { data } = await apiClient.get<CallContact>(`/api/contacts/${userId}`);
    return data;
  },
};
