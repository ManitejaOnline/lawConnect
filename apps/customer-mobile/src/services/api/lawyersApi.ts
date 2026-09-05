import { apiClient } from './client';
import type { Appointment, AppointmentsPage } from '@/types/appointment';
import type {
  AvailableSlot,
  LawyerContact,
  LawyerProfile,
  LawyerSearchItem,
  LawyerSearchParams,
  LawyerWeeklyAvailability,
  PagedResult,
} from '@/types/home';
import { appendPickedFile, type PickedFile } from '@/utils/uploadFile';

function toQuery(params: LawyerSearchParams): Record<string, string | number | boolean> {
  const query: Record<string, string | number | boolean> = {};
  (Object.entries(params) as [keyof LawyerSearchParams, LawyerSearchParams[keyof LawyerSearchParams]][]).forEach(
    ([key, value]) => {
      if (value === undefined || value === null || value === '') return;
      query[key] = value as string | number | boolean;
    },
  );
  return query;
}

export const lawyersApi = {
  search: async (params: LawyerSearchParams = {}) => {
    const { data } = await apiClient.get<PagedResult<LawyerSearchItem>>('/api/lawyers/search', {
      params: toQuery({ pageNumber: 1, pageSize: 20, ...params }),
    });
    return data;
  },

  getById: async (lawyerId: string) => {
    const { data } = await apiClient.get<LawyerProfile>(`/api/lawyers/${lawyerId}`);
    return data;
  },

  getContact: async (lawyerId: string) => {
    const { data } = await apiClient.get<LawyerContact>(`/api/lawyers/${lawyerId}/contact`);
    return data;
  },

  getMine: async () => {
    const { data } = await apiClient.get<LawyerProfile>('/api/lawyers/me');
    return data;
  },

  getAvailableSlots: async (
    lawyerId: string,
    date: string,
    timeZoneId = 'Asia/Kolkata',
    slotMinutes = 30,
  ) => {
    const { data } = await apiClient.get<AvailableSlot[]>(`/api/lawyers/${lawyerId}/slots`, {
      params: { date, timeZoneId, slotMinutes },
    });
    return data;
  },

  setOnlineStatus: async (isOnline: boolean) => {
    const { data } = await apiClient.put<LawyerProfile>('/api/lawyers/me/online-status', { isOnline });
    return data;
  },

  setEmergencyAvailability: async (payload: {
    isAvailableForEmergency: boolean;
    emergencyFee?: number | null;
  }) => {
    const { data } = await apiClient.put<LawyerProfile>('/api/lawyers/me/emergency-availability', {
      isAvailableForEmergency: payload.isAvailableForEmergency,
      emergencyFee: payload.emergencyFee ?? null,
    });
    return data;
  },

  updateProfile: async (payload: {
    bio?: string | null;
    barCouncilNumber?: string | null;
    yearsOfExperience: number;
    isAcceptingAppointments: boolean;
  }) => {
    const { data } = await apiClient.put<LawyerProfile>('/api/lawyers/me', payload);
    return data;
  },

  updateFees: async (payload: { consultationFee: number; emergencyFee?: number | null }) => {
    const { data } = await apiClient.put<LawyerProfile>('/api/lawyers/me/fees', payload);
    return data;
  },

  getMyAvailability: async () => {
    const { data } = await apiClient.get<LawyerWeeklyAvailability[]>('/api/lawyers/me/availability');
    return data;
  },

  setMyAvailability: async (slots: {
    dayOfWeek: number;
    startTime: string;
    endTime: string;
    isAvailable: boolean;
    timeZoneId: string;
  }[]) => {
    const { data } = await apiClient.put<LawyerWeeklyAvailability[]>('/api/lawyers/me/availability', {
      slots,
    });
    return data;
  },

  listSpecializations: async () => {
    const { data } = await apiClient.get<{ id: string; name: string; slug: string }[]>(
      '/api/lawyers/specializations',
    );
    return data;
  },

  saveApplication: async (payload: {
    stateBarCouncil?: string;
    barCouncilNumber?: string;
    specializationIds?: string[];
    yearsOfExperience?: number;
    languages?: string;
    education?: string;
    consultationFee?: number;
    cityName?: string;
    stateName?: string;
    countryName?: string;
    bio?: string;
    availability?: {
      dayOfWeek: number;
      startTime: string;
      endTime: string;
      isAvailable: boolean;
      timeZoneId: string;
    }[];
  }) => {
    const { data } = await apiClient.put<LawyerProfile>('/api/lawyers/me/application', payload);
    return data;
  },

  submitApplication: async () => {
    const { data } = await apiClient.post<LawyerProfile>('/api/lawyers/me/application/submit');
    return data;
  },

  reopenApplication: async () => {
    const { data } = await apiClient.post<LawyerProfile>('/api/lawyers/me/application/reopen');
    return data;
  },

  listDocuments: async () => {
    const { data } = await apiClient.get<
      { id: string; documentType: string; fileName: string; downloadPath?: string | null }[]
    >('/api/lawyers/me/documents');
    return data;
  },

  uploadDocument: async (documentType: string, file: PickedFile) => {
    const form = new FormData();
    form.append('documentType', documentType);
    appendPickedFile(form, 'file', file);
    const { data } = await apiClient.post('/api/lawyers/me/documents', form, {
      transformRequest: (body) => body,
    });
    return data;
  },
};

export const appointmentsApi = {
  book: async (payload: {
    lawyerId: string;
    startUtc: string;
    endUtc: string;
    notes?: string;
    timeZoneId?: string;
  }) => {
    const { data } = await apiClient.post<Appointment>('/api/appointments', {
      lawyerId: payload.lawyerId,
      startUtc: payload.startUtc,
      endUtc: payload.endUtc,
      notes: payload.notes ?? null,
      timeZoneId: payload.timeZoneId ?? 'Asia/Kolkata',
    });
    return data;
  },

  getUpcoming: async (pageNumber = 1, pageSize = 50) => {
    const { data } = await apiClient.get<AppointmentsPage>('/api/appointments/upcoming', {
      params: { pageNumber, pageSize },
    });
    return data;
  },

  getHistory: async (pageNumber = 1, pageSize = 50) => {
    const { data } = await apiClient.get<AppointmentsPage>('/api/appointments/history', {
      params: { pageNumber, pageSize },
    });
    return data;
  },

  getById: async (appointmentId: string) => {
    const { data } = await apiClient.get<Appointment>(`/api/appointments/${appointmentId}`);
    return data;
  },

  cancel: async (appointmentId: string, reason?: string) => {
    const { data } = await apiClient.post<Appointment>(`/api/appointments/${appointmentId}/cancel`, {
      reason: reason ?? null,
    });
    return data;
  },

  reschedule: async (
    appointmentId: string,
    payload: { startUtc: string; endUtc: string; timeZoneId: string },
  ) => {
    const { data } = await apiClient.post<Appointment>(`/api/appointments/${appointmentId}/reschedule`, payload);
    return data;
  },

  complete: async (appointmentId: string) => {
    const { data } = await apiClient.post<Appointment>(`/api/appointments/${appointmentId}/complete`);
    return data;
  },

  /** Reports that the lawyer did not attend. Only callable after the scheduled end time. */
  reportNoShow: async (appointmentId: string) => {
    const { data } = await apiClient.post<Appointment>(`/api/appointments/${appointmentId}/report-no-show`);
    return data;
  },
};
