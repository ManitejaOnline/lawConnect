import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { adminApi } from '@/services/api/adminApi';
import type { ComplaintSeverity, ComplaintStatus, VerificationStatus } from '@/types/api';
import type { CaseStatus } from '@/types/case';

export const adminKeys = {
  applications: (status: VerificationStatus | VerificationStatus[] | 'all', pageNumber: number) =>
    ['admin', 'applications', status, pageNumber] as const,
  application: (lawyerId: string) => ['admin', 'application', lawyerId] as const,
  trustScore: (lawyerId: string) => ['admin', 'trust-score', lawyerId] as const,
  complaints: (status: ComplaintStatus | 'all', pageNumber: number) =>
    ['admin', 'complaints', status, pageNumber] as const,
  cases: (status: CaseStatus | 'all', overdueOnly: boolean, inactiveOnly: boolean, stallingOnly: boolean, pageNumber: number) =>
    ['admin', 'cases', status, overdueOnly, inactiveOnly, stallingOnly, pageNumber] as const,
  case: (caseId: string) => ['admin', 'case', caseId] as const,
  lawyerCaseActivity: (lawyerId: string) => ['admin', 'lawyer-case-activity', lawyerId] as const,
};

export function useAdminApplicationsQuery(
  status?: VerificationStatus | VerificationStatus[],
  pageNumber = 1,
  pageSize = 20,
) {
  return useQuery({
    queryKey: adminKeys.applications(status ?? 'all', pageNumber),
    queryFn: () => adminApi.listApplications({ status, pageNumber, pageSize }),
  });
}

export function useAdminApplicationQuery(lawyerId: string) {
  return useQuery({
    queryKey: adminKeys.application(lawyerId),
    queryFn: () => adminApi.getApplication(lawyerId),
    enabled: Boolean(lawyerId),
  });
}

function useInvalidateAdminLists() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['admin', 'applications'] });
}

export function useApproveLawyerMutation() {
  const queryClient = useQueryClient();
  const invalidateLists = useInvalidateAdminLists();
  return useMutation({
    mutationFn: (lawyerId: string) => adminApi.approve(lawyerId),
    onSuccess: (detail) => {
      queryClient.setQueryData(adminKeys.application(detail.lawyerId), detail);
      void invalidateLists();
    },
  });
}

export function useRejectLawyerMutation() {
  const queryClient = useQueryClient();
  const invalidateLists = useInvalidateAdminLists();
  return useMutation({
    mutationFn: ({ lawyerId, reason }: { lawyerId: string; reason: string }) =>
      adminApi.reject(lawyerId, reason),
    onSuccess: (detail) => {
      queryClient.setQueryData(adminKeys.application(detail.lawyerId), detail);
      void invalidateLists();
    },
  });
}

export function useAdminLawyerTrustScoreQuery(lawyerId: string) {
  return useQuery({
    queryKey: adminKeys.trustScore(lawyerId),
    queryFn: () => adminApi.getLawyerTrustScore(lawyerId),
    enabled: Boolean(lawyerId),
  });
}

export function useAdminComplaintsQuery(status?: ComplaintStatus, pageNumber = 1, pageSize = 20) {
  return useQuery({
    queryKey: adminKeys.complaints(status ?? 'all', pageNumber),
    queryFn: () => adminApi.listComplaints({ status, pageNumber, pageSize }),
  });
}

function useInvalidateComplaintLists() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['admin', 'complaints'] });
}

export function useVerifyComplaintMutation() {
  const invalidateLists = useInvalidateComplaintLists();
  return useMutation({
    mutationFn: ({
      complaintId,
      severity,
      notes,
    }: {
      complaintId: string;
      severity: ComplaintSeverity;
      notes?: string;
    }) => adminApi.verifyComplaint(complaintId, severity, notes),
    onSuccess: () => void invalidateLists(),
  });
}

export function useDismissComplaintMutation() {
  const invalidateLists = useInvalidateComplaintLists();
  return useMutation({
    mutationFn: ({ complaintId, notes }: { complaintId: string; notes?: string }) =>
      adminApi.dismissComplaint(complaintId, notes),
    onSuccess: () => void invalidateLists(),
  });
}

export function useAdminCasesQuery(
  status?: CaseStatus,
  overdueOnly = false,
  inactiveOnly = false,
  stallingOnly = false,
  pageNumber = 1,
  pageSize = 20,
) {
  return useQuery({
    queryKey: adminKeys.cases(status ?? 'all', overdueOnly, inactiveOnly, stallingOnly, pageNumber),
    queryFn: () => adminApi.listCases({ status, overdueOnly, inactiveOnly, stallingOnly, pageNumber, pageSize }),
  });
}

export function useAdminCaseQuery(caseId: string) {
  return useQuery({
    queryKey: adminKeys.case(caseId),
    queryFn: () => adminApi.getCase(caseId),
    enabled: Boolean(caseId),
  });
}

export function useAdminLawyerCaseActivityQuery(lawyerId: string) {
  return useQuery({
    queryKey: adminKeys.lawyerCaseActivity(lawyerId),
    queryFn: () => adminApi.getLawyerCaseActivity(lawyerId),
    enabled: Boolean(lawyerId),
  });
}
