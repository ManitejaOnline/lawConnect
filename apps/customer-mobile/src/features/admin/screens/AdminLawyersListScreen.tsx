import React, { useState } from 'react';
import { FlatList, Pressable, RefreshControl, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { AppModal } from '@/components/ui/Modal';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import {
  useAdminApplicationsQuery,
  useApproveLawyerMutation,
  useRejectLawyerMutation,
} from '@/features/admin/hooks/useAdminQueries';
import { isReviewable, statusTone } from '@/features/admin/statusTone';
import type { AdminLawyersStackParamList } from '@/navigation/types';
import type { AdminLawyerApplicationSummary, VerificationStatus } from '@/types/api';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

type Nav = NativeStackNavigationProp<AdminLawyersStackParamList, 'AdminLawyersList'>;

export function AdminLawyersListScreen({
  title,
  status,
}: {
  title: string;
  status: VerificationStatus | VerificationStatus[];
}) {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const showToast = useToastStore((s) => s.show);
  const [pageNumber, setPageNumber] = useState(1);
  const [rejectTarget, setRejectTarget] = useState<AdminLawyerApplicationSummary | null>(null);
  const [rejectReason, setRejectReason] = useState('');

  const query = useAdminApplicationsQuery(status, pageNumber, 20);
  const approve = useApproveLawyerMutation();
  const reject = useRejectLawyerMutation();

  const closeRejectModal = () => {
    setRejectTarget(null);
    setRejectReason('');
  };

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title={title} subtitle={`${query.data?.totalCount ?? 0} total`} />

        {query.isLoading ? (
          <View style={{ gap: theme.spacing.sm }}>
            {Array.from({ length: 6 }).map((_, i) => (
              <Shimmer key={i} height={76} radius={theme.radius.lg} />
            ))}
          </View>
        ) : query.isError ? (
          <ErrorState message="Could not load lawyers." onRetry={() => query.refetch()} />
        ) : (
          <FlatList
            data={query.data?.items ?? []}
            keyExtractor={(item) => item.lawyerId}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'] }}
            refreshControl={
              <RefreshControl refreshing={query.isRefetching} onRefresh={() => query.refetch()} tintColor={theme.colors.accent} />
            }
            onEndReachedThreshold={0.4}
            onEndReached={() => {
              if (query.data?.hasNextPage) setPageNumber((p) => p + 1);
            }}
            ListEmptyComponent={
              <EmptyState variant="inbox" title="No lawyers here" description="Nothing in this status yet." />
            }
            renderItem={({ item }) => (
              <View
                style={{
                  padding: theme.spacing.md,
                  borderRadius: theme.radius.lg,
                  borderWidth: 1,
                  borderColor: theme.colors.border,
                  backgroundColor: theme.colors.surface,
                  gap: 4,
                }}
              >
                <Pressable
                  accessibilityRole="button"
                  onPress={() => navigation.navigate('AdminLawyerDetail', { lawyerId: item.lawyerId })}
                  style={{ gap: 4 }}
                >
                  <View style={{ flexDirection: 'row', justifyContent: 'space-between', gap: theme.spacing.sm }}>
                    <Text variant="bodyMedium" style={{ flex: 1 }}>
                      {item.fullName}
                    </Text>
                    <Badge label={item.verificationStatus} tone={statusTone(item.verificationStatus)} />
                  </View>
                  <Text variant="caption" color="secondary">
                    {item.email ?? '—'}
                  </Text>
                  <Text variant="caption" color="secondary">
                    {item.phoneNumber}
                  </Text>
                  {item.practiceAreas.length > 0 ? (
                    <Text variant="caption" color="secondary">
                      {item.practiceAreas.join(', ')}
                    </Text>
                  ) : null}
                  <Text variant="label" color="tertiary">
                    {item.stateBarCouncil ?? '—'} {item.enrollmentNumber ? `/ ${item.enrollmentNumber}` : ''} ·{' '}
                    {new Date(item.applicationSubmittedAtUtc ?? item.createdAtUtc).toLocaleDateString()}
                  </Text>
                  {item.verificationStatus === 'Approved' && item.verifiedAtUtc ? (
                    <Text variant="label" color="secondary">
                      Approved {new Date(item.verifiedAtUtc).toLocaleString()}
                    </Text>
                  ) : null}
                  {item.verificationStatus === 'Rejected' && item.reviewedAtUtc ? (
                    <Text variant="label" color="secondary">
                      Rejected {new Date(item.reviewedAtUtc).toLocaleString()}
                      {item.reviewReason ? ` — ${item.reviewReason}` : ''}
                    </Text>
                  ) : null}
                </Pressable>

                {isReviewable(item.verificationStatus) ? (
                  <View style={{ flexDirection: 'row', gap: theme.spacing.xs, marginTop: theme.spacing.xs }}>
                    <View style={{ flex: 1 }}>
                      <Button
                        title="Approve"
                        variant="primary"
                        size="sm"
                        loading={approve.isPending}
                        onPress={() =>
                          approve.mutate(item.lawyerId, {
                            onSuccess: () => showToast({ title: 'Lawyer approved', variant: 'success' }),
                            onError: (error) =>
                              showToast({
                                title: 'Approve failed',
                                message: getErrorMessage(error, 'Could not approve lawyer'),
                                variant: 'error',
                              }),
                          })
                        }
                      />
                    </View>
                    <View style={{ flex: 1 }}>
                      <Button
                        title="Reject"
                        variant="danger"
                        size="sm"
                        onPress={() => setRejectTarget(item)}
                      />
                    </View>
                  </View>
                ) : null}
              </View>
            )}
          />
        )}
      </View>

      <AppModal
        visible={!!rejectTarget}
        onClose={closeRejectModal}
        title={`Reject ${rejectTarget?.fullName ?? ''}`}
        footer={
          <>
            <Button
              title="Reject"
              variant="danger"
              disabled={!rejectReason.trim()}
              loading={reject.isPending}
              onPress={() => {
                if (!rejectTarget) return;
                reject.mutate(
                  { lawyerId: rejectTarget.lawyerId, reason: rejectReason.trim() },
                  {
                    onSuccess: () => {
                      closeRejectModal();
                      showToast({ title: 'Lawyer rejected', variant: 'success' });
                    },
                    onError: (error) =>
                      showToast({
                        title: 'Reject failed',
                        message: getErrorMessage(error, 'Could not reject lawyer'),
                        variant: 'error',
                      }),
                  },
                );
              }}
            />
            <Button title="Cancel" variant="ghost" onPress={closeRejectModal} />
          </>
        }
      >
        <Text variant="body" color="secondary">
          A reason is required and will be shown to the lawyer.
        </Text>
        <TextField
          label="Reason"
          value={rejectReason}
          onChangeText={setRejectReason}
          multiline
          containerStyle={{ marginTop: theme.spacing.md }}
        />
      </AppModal>
    </Screen>
  );
}
