import { apiClient } from '@/services/api/client';

export type CustomerDocumentItem = {
  documentId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  caseId?: string | null;
  appointmentId?: string | null;
  createdAtUtc: string;
  openTransferId?: string | null;
  openTransferStatus?: string | null;
};

export type ApprovedTransfer = {
  sessionId: string;
  uploadToken: string;
  expiresAtUtc: string;
  uploadPath: string;
};

export const customerDocumentsApi = {
  register: async (input: {
    caseId?: string;
    appointmentId?: string;
    fileName: string;
    contentType: string;
    fileSizeBytes: number;
  }) => {
    const { data } = await apiClient.post<{ documentId: string; fileName: string; contentType: string; fileSizeBytes: number }>(
      '/api/customer-documents',
      input,
    );
    return data;
  },

  list: async (scope: { caseId?: string; appointmentId?: string }) => {
    const { data } = await apiClient.get<CustomerDocumentItem[]>('/api/customer-documents', { params: scope });
    return data;
  },

  request: async (documentId: string) => {
    const { data } = await apiClient.post<{ sessionId: string; status: string; expiresAtUtc: string }>(
      `/api/customer-documents/${documentId}/requests`,
    );
    return data;
  },

  approve: async (sessionId: string) => {
    const { data } = await apiClient.post<ApprovedTransfer>(`/api/customer-documents/transfers/${sessionId}/approve`);
    return data;
  },

  reject: async (sessionId: string) => {
    await apiClient.post(`/api/customer-documents/transfers/${sessionId}/reject`);
  },
};
