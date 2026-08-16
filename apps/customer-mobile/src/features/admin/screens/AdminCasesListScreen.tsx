import React, { useState } from 'react';
import { FlatList, Pressable, RefreshControl, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Badge, type BadgeProps } from '@/components/ui/Badge';
import { Chip } from '@/components/ui/Chip';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useAdminCasesQuery } from '@/features/admin/hooks/useAdminQueries';
import type { AdminCasesStackParamList } from '@/navigation/types';
import type { CaseStatus } from '@/types/case';
import { useTheme } from '@/theme/ThemeProvider';

type Nav = NativeStackNavigationProp<AdminCasesStackParamList, 'AdminCasesList'>;

const STATUS_TABS: { label: string; status: CaseStatus | undefined }[] = [
  { label: 'All', status: undefined },
  { label: 'Active', status: 'Active' },
  { label: 'OnHold', status: 'OnHold' },
  { label: 'Closed', status: 'Closed' },
];

function statusTone(status: CaseStatus): NonNullable<BadgeProps['tone']> {
  switch (status) {
    case 'Won':
    case 'Settled':
    case 'Closed':
      return 'success';
    case 'Lost':
    case 'Cancelled':
      return 'danger';
    case 'OnHold':
      return 'warning';
    case 'Active':
      return 'accent';
    default:
      return 'neutral';
  }
}

function stallingTone(status: string): NonNullable<BadgeProps['tone']> {
  return status === 'SevereStalling' ? 'danger' : status === 'Stalling' ? 'warning' : 'neutral';
}

function stallingLabel(status: string): string {
  return status === 'SevereStalling' ? 'Severely stalling' : status;
}

export function AdminCasesListScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const [status, setStatus] = useState<CaseStatus | undefined>(undefined);
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [inactiveOnly, setInactiveOnly] = useState(false);
  const [stallingOnly, setStallingOnly] = useState(false);

  const query = useAdminCasesQuery(status, overdueOnly, inactiveOnly, stallingOnly, 1, 50);

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title="Cases" subtitle={`${query.data?.totalCount ?? 0} matching`} />

        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs, marginBottom: theme.spacing.xs }}>
          {STATUS_TABS.map((t) => (
            <Chip key={t.label} label={t.label} selected={status === t.status} onPress={() => setStatus(t.status)} />
          ))}
        </View>
        <View style={{ flexDirection: 'row', gap: theme.spacing.xs, marginBottom: theme.spacing.sm }}>
          <Chip label="Overdue only" selected={overdueOnly} onPress={() => setOverdueOnly((v) => !v)} icon="alert" />
          <Chip label="Inactive only" selected={inactiveOnly} onPress={() => setInactiveOnly((v) => !v)} icon="clock" />
          <Chip label="Stalling only" selected={stallingOnly} onPress={() => setStallingOnly((v) => !v)} icon="alert" />
        </View>

        {query.isLoading ? (
          <View style={{ gap: theme.spacing.sm }}>
            {Array.from({ length: 5 }).map((_, i) => (
              <Shimmer key={i} height={90} radius={theme.radius.lg} />
            ))}
          </View>
        ) : query.isError ? (
          <ErrorState message="Could not load cases." onRetry={() => query.refetch()} />
        ) : (
          <FlatList
            data={query.data?.items ?? []}
            keyExtractor={(item) => item.id}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'] }}
            refreshControl={<RefreshControl refreshing={query.isRefetching} onRefresh={() => query.refetch()} tintColor={theme.colors.accent} />}
            ListEmptyComponent={<EmptyState variant="inbox" title="No cases" description="Nothing matches these filters." />}
            renderItem={({ item }) => (
              <Pressable
                accessibilityRole="button"
                onPress={() => navigation.navigate('AdminCaseDetail', { caseId: item.id })}
                style={{
                  padding: theme.spacing.md,
                  borderRadius: theme.radius.lg,
                  borderWidth: 1,
                  borderColor: theme.colors.border,
                  backgroundColor: theme.colors.surface,
                  gap: 4,
                }}
              >
                <View style={{ flexDirection: 'row', justifyContent: 'space-between', gap: theme.spacing.sm }}>
                  <Text variant="bodyMedium" style={{ flex: 1 }}>
                    {item.title}
                  </Text>
                  <Badge label={item.status} tone={statusTone(item.status)} />
                </View>
                <Text variant="caption" color="secondary">
                  {item.lawyerName} · {item.customerName} · {item.caseType}
                </Text>
                <View style={{ flexDirection: 'row', gap: theme.spacing.sm, alignItems: 'center', flexWrap: 'wrap' }}>
                  <Text variant="label" color="tertiary">
                    {item.daysSinceLastUpdate == null ? 'No updates yet' : `Updated ${item.daysSinceLastUpdate}d ago`}
                  </Text>
                  {item.overdueMilestoneCount > 0 ? (
                    <Text variant="label" color="danger">
                      {item.overdueMilestoneCount} overdue
                    </Text>
                  ) : null}
                  {item.stallingStatus === 'Stalling' || item.stallingStatus === 'SevereStalling' ? (
                    <Badge label={`${stallingLabel(item.stallingStatus)} · ${item.daysSinceLastMeaningfulLawyerActivity}d`} tone={stallingTone(item.stallingStatus)} />
                  ) : null}
                </View>
              </Pressable>
            )}
          />
        )}
      </View>
    </Screen>
  );
}
