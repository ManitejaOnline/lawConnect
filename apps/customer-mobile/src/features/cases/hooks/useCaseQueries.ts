import type { AxiosProgressEvent } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { casesApi } from '@/services/api/casesApi';
import type { CasePriority, CaseStatus, CaseUpdateType, HearingStatus, MilestoneStatus } from '@/types/case';
import type { PickedFile } from '@/utils/uploadFile';

export const caseKeys = {
  all: ['cases'] as const,
  list: (status: CaseStatus | 'all', pageNumber: number) => [...caseKeys.all, 'list', status, pageNumber] as const,
  detail: (caseId: string) => [...caseKeys.all, 'detail', caseId] as const,
  timeline: (caseId: string) => [...caseKeys.all, 'timeline', caseId] as const,
  updates: (caseId: string) => [...caseKeys.all, 'updates', caseId] as const,
  hearings: (caseId: string) => [...caseKeys.all, 'hearings', caseId] as const,
  milestones: (caseId: string) => [...caseKeys.all, 'milestones', caseId] as const,
  documents: (caseId: string) => [...caseKeys.all, 'documents', caseId] as const,
};

export function useCasesQuery(status?: CaseStatus, pageNumber = 1, pageSize = 20) {
  return useQuery({
    queryKey: caseKeys.list(status ?? 'all', pageNumber),
    queryFn: () => casesApi.list({ status, pageNumber, pageSize }),
  });
}

export function useCaseDetailQuery(caseId: string) {
  return useQuery({
    queryKey: caseKeys.detail(caseId),
    queryFn: () => casesApi.getById(caseId),
    enabled: Boolean(caseId),
  });
}

export function useCaseTimelineQuery(caseId: string) {
  return useQuery({
    queryKey: caseKeys.timeline(caseId),
    queryFn: () => casesApi.getTimeline(caseId),
    enabled: Boolean(caseId),
  });
}

export function useCaseUpdatesQuery(caseId: string) {
  return useQuery({
    queryKey: caseKeys.updates(caseId),
    queryFn: () => casesApi.listUpdates(caseId),
    enabled: Boolean(caseId),
  });
}

export function useCaseHearingsQuery(caseId: string) {
  return useQuery({
    queryKey: caseKeys.hearings(caseId),
    queryFn: () => casesApi.listHearings(caseId),
    enabled: Boolean(caseId),
  });
}

export function useCaseMilestonesQuery(caseId: string) {
  return useQuery({
    queryKey: caseKeys.milestones(caseId),
    queryFn: () => casesApi.listMilestones(caseId),
    enabled: Boolean(caseId),
  });
}

export function useCaseDocumentsQuery(caseId: string) {
  return useQuery({
    queryKey: caseKeys.documents(caseId),
    queryFn: () => casesApi.listDocuments(caseId),
    enabled: Boolean(caseId),
  });
}

function useInvalidateCase(caseId: string) {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: caseKeys.detail(caseId) });
    void queryClient.invalidateQueries({ queryKey: caseKeys.timeline(caseId) });
    void queryClient.invalidateQueries({ queryKey: caseKeys.all });
  };
}

export function useCreateCaseMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: {
      customerId: string;
      specializationId: string;
      title: string;
      caseNumber?: string | null;
      description?: string | null;
      courtName?: string | null;
      priority: CasePriority;
      startDate: string;
      sourceAppointmentId?: string | null;
    }) => casesApi.create(payload),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: caseKeys.all }),
  });
}

export function useChangeCaseStatusMutation(caseId: string) {
  const invalidate = useInvalidateCase(caseId);
  return useMutation({
    mutationFn: ({ status, note }: { status: CaseStatus; note?: string }) => casesApi.changeStatus(caseId, status, note),
    onSuccess: () => invalidate(),
  });
}

export function useCreateCaseUpdateMutation(caseId: string) {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateCase(caseId);
  return useMutation({
    mutationFn: ({ updateType, title, description }: { updateType: CaseUpdateType; title: string; description: string }) =>
      casesApi.createUpdate(caseId, updateType, title, description),
    onSuccess: () => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: caseKeys.updates(caseId) });
    },
  });
}

export function useCreateHearingMutation(caseId: string) {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateCase(caseId);
  return useMutation({
    mutationFn: (payload: { hearingDate: string; courtName?: string | null; hearingType?: string | null; purpose?: string | null }) =>
      casesApi.createHearing(caseId, payload),
    onSuccess: () => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: caseKeys.hearings(caseId) });
    },
  });
}

export function useRecordHearingOutcomeMutation(caseId: string) {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateCase(caseId);
  return useMutation({
    mutationFn: ({
      hearingId,
      ...payload
    }: {
      hearingId: string;
      status: HearingStatus;
      outcome?: string | null;
      notes?: string | null;
      nextAction?: string | null;
    }) => casesApi.recordHearingOutcome(caseId, hearingId, payload),
    onSuccess: () => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: caseKeys.hearings(caseId) });
    },
  });
}

export function useCreateMilestoneMutation(caseId: string) {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateCase(caseId);
  return useMutation({
    mutationFn: (payload: { title: string; description?: string | null; dueDate?: string | null }) =>
      casesApi.createMilestone(caseId, payload),
    onSuccess: () => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: caseKeys.milestones(caseId) });
    },
  });
}

export function useCompleteMilestoneMutation(caseId: string) {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateCase(caseId);
  return useMutation({
    mutationFn: (milestoneId: string) => casesApi.completeMilestone(caseId, milestoneId),
    onSuccess: () => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: caseKeys.milestones(caseId) });
    },
  });
}

export function useUploadCaseDocumentMutation(caseId: string) {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateCase(caseId);
  return useMutation({
    mutationFn: ({
      file,
      scope,
      onUploadProgress,
    }: {
      file: PickedFile;
      scope?: { caseUpdateId?: string; hearingId?: string; milestoneId?: string };
      onUploadProgress?: (event: AxiosProgressEvent) => void;
    }) => casesApi.uploadDocument(caseId, file, scope, onUploadProgress),
    onSuccess: () => {
      invalidate();
      void queryClient.invalidateQueries({ queryKey: caseKeys.documents(caseId) });
    },
  });
}

export function useDeleteCaseDocumentMutation(caseId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => casesApi.deleteDocument(caseId, documentId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: caseKeys.documents(caseId) }),
  });
}

export function useSubmitCaseRatingMutation(caseId: string) {
  const invalidate = useInvalidateCase(caseId);
  return useMutation({
    mutationFn: ({ stars, reviewText }: { stars: number; reviewText?: string | null }) =>
      casesApi.submitRating(caseId, stars, reviewText),
    onSuccess: () => invalidate(),
  });
}
