import React from 'react';
import { View } from 'react-native';
import { useMutation, useQueryClient } from '@tanstack/react-query';

import { Button } from '@/components/ui/Button';
import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { lawyerKeys, useMyLawyerProfileQuery } from '@/features/lawyer/hooks/useLawyerQueries';
import { authApi } from '@/services/api/authApi';
import { lawyersApi } from '@/services/api/lawyersApi';
import { useAuthStore } from '@/store/authStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

export function LawyerPendingScreen() {
  const theme = useTheme();
  const queryClient = useQueryClient();
  const showToast = useToastStore((s) => s.show);
  const userId = useAuthStore((s) => s.session?.userId);
  const mine = useMyLawyerProfileQuery();
  const status = (mine.data?.verificationStatus ?? '').toLowerCase();
  const rejected = status === 'rejected';
  const suspended = status === 'suspended';

  const logout = useMutation({
    mutationFn: () => authApi.logout(),
  });

  const reopen = useMutation({
    mutationFn: lawyersApi.reopenApplication,
    onSuccess: (profile) => {
      queryClient.setQueryData(lawyerKeys.me(userId), profile);
      showToast({ title: 'Application reopened', message: 'Update your details and resubmit.', variant: 'success' });
    },
    onError: (error) => {
      showToast({
        title: 'Could not reopen',
        message: getErrorMessage(error, 'Try again later'),
        variant: 'error',
      });
    },
  });

  const title = rejected || suspended ? 'Verification required' : 'Verification pending';
  const body = rejected
    ? mine.data?.reviewReason
      ? `Your application was not approved. ${mine.data.reviewReason}`
      : 'Your application was not approved. Update your details and resubmit.'
    : suspended
      ? mine.data?.reviewReason || 'This lawyer account is suspended. Contact support.'
      : 'Your application has been submitted for verification. Status: Pending Verification. You cannot appear as a verified lawyer, accept SOS, or go online until an admin approves your account.';

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']}>
      <View style={{ flex: 1, justifyContent: 'center', gap: theme.spacing.lg }}>
        <View
          style={{
            width: 72,
            height: 72,
            borderRadius: theme.radius.xl,
            backgroundColor: theme.colors.accentSoft,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Icon name={rejected || suspended ? 'alert' : 'clock'} size={32} color={theme.colors.accent} />
        </View>
        <View style={{ gap: theme.spacing.sm }}>
          <Text variant="label" color="accent">
            LAWYER APP
          </Text>
          <Text variant="title">{title}</Text>
          <Text variant="body" color="secondary">
            {body}
          </Text>
          {!rejected && !suspended && mine.data?.applicationSubmittedAtUtc ? (
            <Text variant="caption" color="secondary">
              Submitted {new Date(mine.data.applicationSubmittedAtUtc).toLocaleString()}
            </Text>
          ) : null}
        </View>
        {rejected ? (
          <Button title="Update and resubmit" variant="primary" loading={reopen.isPending} onPress={() => reopen.mutate()} />
        ) : null}
        <Button title="Sign out" variant="outline" loading={logout.isPending} onPress={() => logout.mutate()} />
      </View>
    </Screen>
  );
}
