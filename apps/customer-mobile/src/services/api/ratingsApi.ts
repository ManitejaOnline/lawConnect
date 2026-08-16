import { apiClient } from './client';
import type {
  LawyerComplaintSubmittedResponse,
  LawyerRatingSubmittedResponse,
  LawyerRatingSummaryResponse,
  LawyerTrustScoreResponse,
} from '@/types/api';

export const ratingsApi = {
  submit: async (lawyerId: string, appointmentId: string, stars: number, reviewText?: string) => {
    const { data } = await apiClient.post<LawyerRatingSubmittedResponse>(
      `/api/lawyers/${lawyerId}/ratings`,
      { appointmentId, stars, reviewText: reviewText ?? null },
    );
    return data;
  },

  getRating: async (lawyerId: string) => {
    const { data } = await apiClient.get<LawyerRatingSummaryResponse>(`/api/lawyers/${lawyerId}/rating`);
    return data;
  },

  getTrustScore: async (lawyerId: string) => {
    const { data } = await apiClient.get<LawyerTrustScoreResponse>(`/api/lawyers/${lawyerId}/trust-score`);
    return data;
  },

  submitComplaint: async (
    lawyerId: string,
    category: string,
    description: string,
    appointmentId?: string,
  ) => {
    const { data } = await apiClient.post<LawyerComplaintSubmittedResponse>(
      `/api/lawyers/${lawyerId}/complaints`,
      { appointmentId: appointmentId ?? null, category, description },
    );
    return data;
  },
};
