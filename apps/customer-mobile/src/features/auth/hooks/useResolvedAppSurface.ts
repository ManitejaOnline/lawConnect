import { useQuery } from '@tanstack/react-query';

import {
  hasRole,
  resolveAppSurface,
  type AppSurface,
  AppRoles,
} from '@/auth/roles';
import { lawyersApi } from '@/services/api/lawyersApi';
import { useAuthStore } from '@/store/authStore';
import { AppError } from '@/utils/errors';

export function useResolvedAppSurface(): { surface: AppSurface; ready: boolean } {
  const session = useAuthStore((s) => s.session);
  const profile = useAuthStore((s) => s.profile);
  const intendedAudience = useAuthStore((s) => s.intendedAudience);
  const roles = profile?.roles ?? session?.roles ?? [];
  const authenticated = Boolean(session?.accessToken);

  const needsLawyerProfile =
    authenticated &&
    !hasRole(roles, AppRoles.Admin) &&
    (intendedAudience === 'lawyer' ||
      (intendedAudience == null && hasRole(roles, AppRoles.Lawyer) && !hasRole(roles, AppRoles.Customer))) &&
    hasRole(roles, AppRoles.Lawyer);

  const mine = useQuery({
    queryKey: ['lawyers', 'me', session?.userId],
    queryFn: lawyersApi.getMine,
    enabled: needsLawyerProfile,
    retry: false,
  });

  if (!authenticated) {
    return { surface: 'auth', ready: true };
  }

  if (needsLawyerProfile && mine.isPending) {
    return { surface: 'auth', ready: false };
  }

  const missing =
    mine.isError && mine.error instanceof AppError ? mine.error.status === 404 : mine.isError;

  return {
    surface: resolveAppSurface({
      authenticated: true,
      intendedAudience,
      roles,
      lawyerVerificationStatus: mine.data?.verificationStatus,
      lawyerIsVerified: mine.data?.isVerified,
      lawyerProfileMissing: missing,
      lawyerApplicationSubmittedAtUtc: mine.data?.applicationSubmittedAtUtc,
    }),
    ready: true,
  };
}
