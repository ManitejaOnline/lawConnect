import React from 'react';
import { Pressable, RefreshControl, ScrollView, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Badge } from '@/components/ui/Badge';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useAdminApplicationsQuery } from '@/features/admin/hooks/useAdminQueries';
import { AWAITING_DECISION_STATUSES, statusTone } from '@/features/admin/statusTone';
import type { AdminDashboardStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

type Nav = NativeStackNavigationProp<AdminDashboardStackParamList, 'AdminDashboard'>;

function StatCard({ label, value, tone }: { label: string; value: number | string; tone?: string }) {
  const theme = useTheme();
  return (
    <View
      style={{
        flexBasis: '48%',
        flexGrow: 1,
        borderWidth: 1,
        borderColor: theme.colors.border,
        borderRadius: theme.radius.lg,
        padding: theme.spacing.md,
        backgroundColor: theme.colors.surface,
      }}
    >
      <Text variant="caption" color="secondary">
        {label}
      </Text>
      <Text variant="display" style={{ color: tone ?? theme.colors.textPrimary, marginTop: 4 }}>
        {value}
      </Text>
    </View>
  );
}

export function AdminDashboardScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();

  const recent = useAdminApplicationsQuery(undefined, 1, 5);
  const pending = useAdminApplicationsQuery(AWAITING_DECISION_STATUSES, 1, 1);
  const approved = useAdminApplicationsQuery('Approved', 1, 1);
  const rejected = useAdminApplicationsQuery('Rejected', 1, 1);

  const loading = recent.isLoading || pending.isLoading || approved.isLoading || rejected.isLoading;
  const refreshing = recent.isRefetching;

  const refetchAll = () => {
    void recent.refetch();
    void pending.refetch();
    void approved.refetch();
    void rejected.refetch();
  };

  return (
    <Screen edges={['top', 'left', 'right']} scroll={false} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title="Admin Dashboard" subtitle="LawConnect" />

        <ScrollView
          showsVerticalScrollIndicator={false}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refetchAll} tintColor={theme.colors.accent} />}
          contentContainerStyle={{ paddingBottom: theme.spacing['3xl'], gap: theme.spacing.lg }}
        >
          {loading ? (
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.sm }}>
              {Array.from({ length: 4 }).map((_, i) => (
                <Shimmer key={i} height={84} radius={theme.radius.lg} style={{ flexBasis: '48%', flexGrow: 1 }} />
              ))}
            </View>
          ) : (
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.sm }}>
              <StatCard label="Total lawyers" value={recent.data?.totalCount ?? 0} />
              <StatCard label="Pending approvals" value={pending.data?.totalCount ?? 0} tone={theme.colors.warning} />
              <StatCard label="Approved lawyers" value={approved.data?.totalCount ?? 0} tone={theme.colors.success} />
              <StatCard label="Rejected lawyers" value={rejected.data?.totalCount ?? 0} tone={theme.colors.danger} />
            </View>
          )}

          <View style={{ gap: theme.spacing.sm }}>
            <Text variant="headline">Recent registrations</Text>
            {!loading && (recent.data?.items.length ?? 0) === 0 ? (
              <EmptyState variant="inbox" title="No registrations yet" />
            ) : (
              recent.data?.items.map((item) => (
                <Pressable
                  key={item.lawyerId}
                  accessibilityRole="button"
                  onPress={() => navigation.navigate('AdminLawyerDetail', { lawyerId: item.lawyerId })}
                  style={{
                    flexDirection: 'row',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    padding: theme.spacing.md,
                    borderRadius: theme.radius.lg,
                    borderWidth: 1,
                    borderColor: theme.colors.border,
                    backgroundColor: theme.colors.surface,
                  }}
                >
                  <View style={{ flex: 1, gap: 2 }}>
                    <Text variant="bodyMedium">{item.fullName}</Text>
                    <Text variant="caption" color="secondary">
                      {item.email ?? item.phoneNumber} ·{' '}
                      {new Date(item.applicationSubmittedAtUtc ?? item.createdAtUtc).toLocaleDateString()}
                    </Text>
                  </View>
                  <Badge label={item.verificationStatus} tone={statusTone(item.verificationStatus)} />
                </Pressable>
              ))
            )}
          </View>
        </ScrollView>
      </View>
    </Screen>
  );
}
