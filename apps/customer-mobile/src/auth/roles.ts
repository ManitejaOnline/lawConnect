export const AppRoles = {
  Customer: 'Customer',
  Lawyer: 'Lawyer',
  Admin: 'Admin',
} as const;

export type AppAudience = 'customer' | 'lawyer';

export type AppSurface =
  | 'auth'
  | 'customer'
  | 'lawyer-home'
  | 'lawyer-onboarding'
  | 'lawyer-pending'
  | 'admin'
  | 'wrong-app';

export function hasRole(roles: readonly string[] | undefined, role: string): boolean {
  return (roles ?? []).some((value) => value.toLowerCase() === role.toLowerCase());
}

export function isLawyerApproved(status: string | null | undefined, isVerified?: boolean): boolean {
  const normalized = (status ?? '').trim().toLowerCase();
  if (normalized === 'approved') return true;
  return Boolean(isVerified) && normalized !== 'rejected' && normalized !== 'suspended';
}

export function resolveAppSurface(input: {
  authenticated: boolean;
  intendedAudience: AppAudience | null;
  roles: readonly string[];
  lawyerVerificationStatus?: string | null;
  lawyerIsVerified?: boolean;
  lawyerProfileMissing?: boolean;
  lawyerApplicationSubmittedAtUtc?: string | null;
}): AppSurface {
  const { authenticated, intendedAudience, roles } = input;
  if (!authenticated) return 'auth';

  if (hasRole(roles, AppRoles.Admin)) {
    return 'admin';
  }

  const audience =
    intendedAudience ??
    (hasRole(roles, AppRoles.Lawyer) && !hasRole(roles, AppRoles.Customer)
      ? 'lawyer'
      : hasRole(roles, AppRoles.Customer)
        ? 'customer'
        : null);

  if (audience === 'customer') {
    return hasRole(roles, AppRoles.Customer) ? 'customer' : 'wrong-app';
  }

  if (audience === 'lawyer') {
    if (!hasRole(roles, AppRoles.Lawyer)) return 'wrong-app';
    if (input.lawyerProfileMissing) return 'lawyer-onboarding';
    if (isLawyerApproved(input.lawyerVerificationStatus, input.lawyerIsVerified)) {
      return 'lawyer-home';
    }
    const status = (input.lawyerVerificationStatus ?? '').trim().toLowerCase();
    if (status === 'rejected' || status === 'suspended' || status === 'underreview') {
      return 'lawyer-pending';
    }
    if (!input.lawyerApplicationSubmittedAtUtc) {
      return 'lawyer-onboarding';
    }
    return 'lawyer-pending';
  }

  return 'wrong-app';
}
