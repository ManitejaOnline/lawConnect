import type { AxiosProgressEvent } from 'axios';
import { apiClient } from './client';
import type { PagedResult } from '@/types/home';
import type {
  CaseDetail,
  CaseDocument,
  CaseItem,
  CaseListItem,
  CaseMilestone,
  CasePriority,
  CaseRating,
  CaseStatus,
  CaseTimelineEntry,
  CaseUpdateItem,
  CaseUpdateType,
  Hearing,
  HearingStatus,
  MilestoneStatus,
} from '@/types/case';
import { appendPickedFile, type PickedFile } from '@/utils/uploadFile';

/**
 * The API has no string-enum JSON converter configured (same quirk as ComplaintSeverity —
 * see types/api.ts) — request bodies must send the numeric wire value for enum fields, even
 * though every response returns the string name. Query-string enum params are unaffected:
 * ASP.NET's query binder accepts names natively, so `list`/admin filters pass strings as-is.
 */
const CASE_PRIORITY_CODE: Record<CasePriority, number> = { Low: 0, Medium: 1, High: 2, Urgent: 3 };
const CASE_STATUS_CODE: Record<CaseStatus, number> = {
  Pending: 0, Active: 1, OnHold: 2, Won: 3, Lost: 4, Settled: 5, Closed: 6, Cancelled: 7,
};
const CASE_UPDATE_TYPE_CODE: Record<CaseUpdateType, number> = {
  General: 0,
  ClientCommunication: 1,
  DocumentFiled: 2,
  DocumentReceived: 3,
  HearingUpdate: 4,
  CourtUpdate: 5,
  Deadline: 6,
  StatusChange: 7,
  Other: 8,
};
const HEARING_STATUS_CODE: Record<HearingStatus, number> = { Scheduled: 0, Completed: 1, Adjourned: 2, Cancelled: 3 };
const MILESTONE_STATUS_CODE: Record<MilestoneStatus, number> = {
  Pending: 0, InProgress: 1, Completed: 2, Overdue: 3, Cancelled: 4,
};

export const casesApi = {
  create: async (payload: {
    customerId: string;
    specializationId: string;
    title: string;
    caseNumber?: string | null;
    description?: string | null;
    courtName?: string | null;
    priority: CasePriority;
    startDate: string;
    sourceAppointmentId?: string | null;
  }) => {
    const { data } = await apiClient.post<CaseItem>('/api/cases', {
      customerId: payload.customerId,
      specializationId: payload.specializationId,
      title: payload.title,
      caseNumber: payload.caseNumber ?? null,
      description: payload.description ?? null,
      courtName: payload.courtName ?? null,
      priority: CASE_PRIORITY_CODE[payload.priority],
      startDate: payload.startDate,
      sourceAppointmentId: payload.sourceAppointmentId ?? null,
    });
    return data;
  },

  list: async (params: { status?: CaseStatus; pageNumber?: number; pageSize?: number } = {}) => {
    const { data } = await apiClient.get<PagedResult<CaseListItem>>('/api/cases', {
      params: { status: params.status, pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 20 },
    });
    return data;
  },

  getById: async (caseId: string) => {
    const { data } = await apiClient.get<CaseDetail>(`/api/cases/${caseId}`);
    return data;
  },

  update: async (
    caseId: string,
    payload: { specializationId: string; title: string; caseNumber?: string | null; description?: string | null; courtName?: string | null; priority: CasePriority },
  ) => {
    const { data } = await apiClient.put<CaseItem>(`/api/cases/${caseId}`, {
      specializationId: payload.specializationId,
      title: payload.title,
      caseNumber: payload.caseNumber ?? null,
      description: payload.description ?? null,
      courtName: payload.courtName ?? null,
      priority: CASE_PRIORITY_CODE[payload.priority],
    });
    return data;
  },

  changeStatus: async (caseId: string, status: CaseStatus, note?: string) => {
    const { data } = await apiClient.post<CaseItem>(`/api/cases/${caseId}/status`, {
      status: CASE_STATUS_CODE[status],
      note: note ?? null,
    });
    return data;
  },

  getTimeline: async (caseId: string) => {
    const { data } = await apiClient.get<CaseTimelineEntry[]>(`/api/cases/${caseId}/timeline`);
    return data;
  },

  createUpdate: async (caseId: string, updateType: CaseUpdateType, title: string, description: string) => {
    const { data } = await apiClient.post<CaseUpdateItem>(`/api/cases/${caseId}/updates`, {
      updateType: CASE_UPDATE_TYPE_CODE[updateType],
      title,
      description,
    });
    return data;
  },

  listUpdates: async (caseId: string) => {
    const { data } = await apiClient.get<CaseUpdateItem[]>(`/api/cases/${caseId}/updates`);
    return data;
  },

  createHearing: async (caseId: string, payload: { hearingDate: string; courtName?: string | null; hearingType?: string | null; purpose?: string | null }) => {
    const { data } = await apiClient.post<Hearing>(`/api/cases/${caseId}/hearings`, {
      hearingDate: payload.hearingDate,
      courtName: payload.courtName ?? null,
      hearingType: payload.hearingType ?? null,
      purpose: payload.purpose ?? null,
    });
    return data;
  },

  updateHearing: async (
    caseId: string,
    hearingId: string,
    payload: { hearingDate: string; courtName?: string | null; hearingType?: string | null; purpose?: string | null },
  ) => {
    const { data } = await apiClient.put<Hearing>(`/api/cases/${caseId}/hearings/${hearingId}`, {
      hearingDate: payload.hearingDate,
      courtName: payload.courtName ?? null,
      hearingType: payload.hearingType ?? null,
      purpose: payload.purpose ?? null,
    });
    return data;
  },

  recordHearingOutcome: async (
    caseId: string,
    hearingId: string,
    payload: { status: HearingStatus; outcome?: string | null; notes?: string | null; nextAction?: string | null },
  ) => {
    const { data } = await apiClient.post<Hearing>(`/api/cases/${caseId}/hearings/${hearingId}/outcome`, {
      status: HEARING_STATUS_CODE[payload.status],
      outcome: payload.outcome ?? null,
      notes: payload.notes ?? null,
      nextAction: payload.nextAction ?? null,
    });
    return data;
  },

  listHearings: async (caseId: string) => {
    const { data } = await apiClient.get<Hearing[]>(`/api/cases/${caseId}/hearings`);
    return data;
  },

  createMilestone: async (caseId: string, payload: { title: string; description?: string | null; dueDate?: string | null }) => {
    const { data } = await apiClient.post<CaseMilestone>(`/api/cases/${caseId}/milestones`, {
      title: payload.title,
      description: payload.description ?? null,
      dueDate: payload.dueDate ?? null,
    });
    return data;
  },

  updateMilestone: async (
    caseId: string,
    milestoneId: string,
    payload: { title: string; description?: string | null; dueDate?: string | null; status: MilestoneStatus },
  ) => {
    const { data } = await apiClient.put<CaseMilestone>(`/api/cases/${caseId}/milestones/${milestoneId}`, {
      title: payload.title,
      description: payload.description ?? null,
      dueDate: payload.dueDate ?? null,
      status: MILESTONE_STATUS_CODE[payload.status],
    });
    return data;
  },

  completeMilestone: async (caseId: string, milestoneId: string) => {
    const { data } = await apiClient.post<CaseMilestone>(`/api/cases/${caseId}/milestones/${milestoneId}/complete`);
    return data;
  },

  listMilestones: async (caseId: string) => {
    const { data } = await apiClient.get<CaseMilestone[]>(`/api/cases/${caseId}/milestones`);
    return data;
  },

  uploadDocument: async (
    caseId: string,
    file: PickedFile,
    scope: { caseUpdateId?: string; hearingId?: string; milestoneId?: string } = {},
    onUploadProgress?: (event: AxiosProgressEvent) => void,
  ) => {
    const form = new FormData();
    if (scope.caseUpdateId) form.append('caseUpdateId', scope.caseUpdateId);
    if (scope.hearingId) form.append('hearingId', scope.hearingId);
    if (scope.milestoneId) form.append('milestoneId', scope.milestoneId);
    appendPickedFile(form, 'file', file);
    const { data } = await apiClient.post<CaseDocument>(`/api/cases/${caseId}/documents`, form, {
      transformRequest: (body) => body,
      onUploadProgress,
    });
    return data;
  },

  listDocuments: async (caseId: string) => {
    const { data } = await apiClient.get<CaseDocument[]>(`/api/cases/${caseId}/documents`);
    return data;
  },

  deleteDocument: async (caseId: string, documentId: string) => {
    await apiClient.delete(`/api/cases/${caseId}/documents/${documentId}`);
  },

  submitRating: async (caseId: string, stars: number, reviewText?: string | null) => {
    const { data } = await apiClient.post<CaseRating>(`/api/cases/${caseId}/rating`, {
      stars,
      reviewText: reviewText ?? null,
    });
    return data;
  },
};
