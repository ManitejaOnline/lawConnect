import { apiClient } from './client';
import type { PagedResult } from '@/types/home';
import type {
  AdminComplaintSummary,
  AdminLawyerApplicationDetail,
  AdminLawyerApplicationSummary,
  AdminLawyerTrustScoreResponse,
  ComplaintSeverity,
  ComplaintStatus,
  VerificationStatus,
} from '@/types/api';
import type { AdminCaseDetail, AdminCaseListItem, CaseStatus, LawyerCaseActivitySummary } from '@/types/case';

export const adminApi = {
  /**
   * Lists lawyer applications. `status` accepts one or several values — pass
   * multiple to match "awaiting an admin decision" across both Pending
   * (registered, application not yet submitted) and UnderReview (submitted,
   * awaiting review) in a single request. Built manually with URLSearchParams
   * so repeated `status=` keys reach ASP.NET's array query-string binding —
   * axios's default array serialization uses `status[]=` instead, which
   * ASP.NET does not bind.
   */
  listApplications: async (params: {
    status?: VerificationStatus | VerificationStatus[];
    pageNumber?: number;
    pageSize?: number;
  } = {}) => {
    const search = new URLSearchParams();
    const statuses = params.status
      ? Array.isArray(params.status)
        ? params.status
        : [params.status]
      : [];
    statuses.forEach((s) => search.append('status', s));
    search.set('pageNumber', String(params.pageNumber ?? 1));
    search.set('pageSize', String(params.pageSize ?? 20));

    const { data } = await apiClient.get<PagedResult<AdminLawyerApplicationSummary>>(
      `/api/admin/lawyers/applications?${search.toString()}`,
    );
    return data;
  },

  getApplication: async (lawyerId: string) => {
    const { data } = await apiClient.get<AdminLawyerApplicationDetail>(
      `/api/admin/lawyers/${lawyerId}/application`,
    );
    return data;
  },

  approve: async (lawyerId: string) => {
    const { data } = await apiClient.post<AdminLawyerApplicationDetail>(
      `/api/admin/lawyers/${lawyerId}/approve`,
    );
    return data;
  },

  reject: async (lawyerId: string, reason: string) => {
    const { data } = await apiClient.post<AdminLawyerApplicationDetail>(
      `/api/admin/lawyers/${lawyerId}/reject`,
      { reason },
    );
    return data;
  },

  getLawyerTrustScore: async (lawyerId: string) => {
    const { data } = await apiClient.get<AdminLawyerTrustScoreResponse>(
      `/api/admin/lawyers/${lawyerId}/trust-score`,
    );
    return data;
  },

  listComplaints: async (params: { status?: ComplaintStatus; pageNumber?: number; pageSize?: number } = {}) => {
    const { data } = await apiClient.get<PagedResult<AdminComplaintSummary>>('/api/admin/complaints', {
      params: {
        status: params.status,
        pageNumber: params.pageNumber ?? 1,
        pageSize: params.pageSize ?? 20,
      },
    });
    return data;
  },

  verifyComplaint: async (complaintId: string, severity: ComplaintSeverity, notes?: string) => {
    const { data } = await apiClient.post<AdminComplaintSummary>(
      `/api/admin/complaints/${complaintId}/verify`,
      { severity, notes: notes ?? null },
    );
    return data;
  },

  dismissComplaint: async (complaintId: string, notes?: string) => {
    const { data } = await apiClient.post<AdminComplaintSummary>(
      `/api/admin/complaints/${complaintId}/dismiss`,
      { notes: notes ?? null },
    );
    return data;
  },

  listCases: async (params: {
    status?: CaseStatus;
    lawyerId?: string;
    customerId?: string;
    overdueOnly?: boolean;
    inactiveOnly?: boolean;
    stallingOnly?: boolean;
    pageNumber?: number;
    pageSize?: number;
  } = {}) => {
    const { data } = await apiClient.get<PagedResult<AdminCaseListItem>>('/api/admin/cases', {
      params: {
        status: params.status,
        lawyerId: params.lawyerId,
        customerId: params.customerId,
        overdueOnly: params.overdueOnly ?? false,
        inactiveOnly: params.inactiveOnly ?? false,
        stallingOnly: params.stallingOnly ?? false,
        pageNumber: params.pageNumber ?? 1,
        pageSize: params.pageSize ?? 20,
      },
    });
    return data;
  },

  getCase: async (caseId: string) => {
    const { data } = await apiClient.get<AdminCaseDetail>(`/api/admin/cases/${caseId}`);
    return data;
  },

  getLawyerCaseActivity: async (lawyerId: string) => {
    const { data } = await apiClient.get<LawyerCaseActivitySummary>(`/api/admin/lawyers/${lawyerId}/case-activity`);
    return data;
  },
};
