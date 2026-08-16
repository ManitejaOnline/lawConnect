import React, { useState } from 'react';
import { FlatList, Pressable, RefreshControl, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Badge } from '@/components/ui/Badge';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useAdminComplaintsQuery } from '@/features/admin/hooks/useAdminQueries';
import type { AdminComplaintsStackParamList } from '@/navigation/types';
import type { ComplaintStatus } from '@/types/api';
import { useTheme } from '@/theme/ThemeProvider';

type Nav = NativeStackNavigationProp<AdminComplaintsStackParamList, 'AdminComplaintsList'>;

const TABS: { label: string; status: ComplaintStatus | undefined }[] = [
  { label: 'Pending', status: 'Pending' },
  { label: 'Verified', status: 'Verified' },
  { label: 'Dismissed', status: 'Dismissed' },
];

function complaintStatusTone(status: ComplaintStatus): 'warning' | 'danger' | 'neutral' {
  switch (status) {
    case 'Pending':
      return 'warning';
    case 'Verified':
      return 'danger';
    default:
      return 'neutral';
  }
}

export function AdminComplaintsListScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const [tab, setTab] = useState<ComplaintStatus>('Pending');

  const query = useAdminComplaintsQuery(tab, 1, 50);

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title="Complaints" subtitle={`${query.data?.totalCount ?? 0} in ${tab.toLowerCase()}`} />

        <View style={{ flexDirection: 'row', gap: theme.spacing.xs, marginBottom: theme.spacing.sm }}>
          {TABS.map(({ label, status }) => (
            <Pressable
              key={label}
              accessibilityRole="button"
              onPress={() => setTab(status as ComplaintStatus)}
              style={{
                flex: 1,
                paddingVertical: 8,
                borderRadius: theme.radius.full,
                alignItems: 'center',
                backgroundColor: tab === status ? theme.colors.primary : theme.colors.surfaceMuted,
              }}
            >
              <Text variant="label" style={{ color: tab === status ? theme.colors.textInverse : theme.colors.textSecondary }}>
                {label}
              </Text>
            </Pressable>
          ))}
        </View>

        {query.isLoading ? (
          <View style={{ gap: theme.spacing.sm }}>
            {Array.from({ length: 5 }).map((_, i) => (
              <Shimmer key={i} height={84} radius={theme.radius.lg} />
            ))}
          </View>
        ) : query.isError ? (
          <ErrorState message="Could not load complaints." onRetry={() => query.refetch()} />
        ) : (
          <FlatList
            data={query.data?.items ?? []}
            keyExtractor={(item) => item.complaintId}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'] }}
            refreshControl={
              <RefreshControl refreshing={query.isRefetching} onRefresh={() => query.refetch()} tintColor={theme.colors.accent} />
            }
            ListEmptyComponent={
              <EmptyState variant="inbox" title="No complaints here" description="Nothing in this status yet." />
            }
            renderItem={({ item }) => (
              <Pressable
                accessibilityRole="button"
                onPress={() => navigation.navigate('AdminComplaintDetail', { complaint: item })}
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
                    {item.category}
                  </Text>
                  <Badge label={item.status} tone={complaintStatusTone(item.status)} />
                </View>
                <Text variant="caption" color="secondary">
                  Against {item.lawyerName} · Filed by {item.clientName}
                </Text>
                <Text variant="caption" color="secondary" numberOfLines={2}>
                  {item.description}
                </Text>
                <Text variant="label" color="tertiary">
                  {new Date(item.createdAtUtc).toLocaleString()}
                </Text>
              </Pressable>
            )}
          />
        )}
      </View>
    </Screen>
  );
}
