import React, { useState } from 'react';
import { RefreshControl, ScrollView, View } from 'react-native';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Badge, type BadgeProps } from '@/components/ui/Badge';
import { Card } from '@/components/ui/Card';
import { Chip } from '@/components/ui/Chip';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useCasesQuery } from '@/features/cases/hooks/useCaseQueries';
import type { CaseStatus } from '@/types/case';
import { useTheme } from '@/theme/ThemeProvider';

const TABS: { label: string; status: CaseStatus | undefined }[] = [
  { label: 'All', status: undefined },
  { label: 'Active', status: 'Active' },
  { label: 'Pending', status: 'Pending' },
  { label: 'On Hold', status: 'OnHold' },
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

export function CasesListScreen({
  perspective,
  onOpen,
  onBack,
}: {
  perspective: 'lawyer' | 'customer';
  onOpen: (caseId: string) => void;
  onBack?: () => void;
}) {
  const theme = useTheme();
  const [tab, setTab] = useState<CaseStatus | undefined>(undefined);
  const query = useCasesQuery(tab, 1, 50);

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter }}>
        <ScreenHeader
          title="My Cases"
          subtitle={perspective === 'lawyer' ? 'Matters you are handling' : 'Your legal matters'}
          onBack={onBack}
        />
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs, marginBottom: theme.spacing.md }}>
          {TABS.map((entry) => (
            <Chip key={entry.label} label={entry.label} selected={tab === entry.status} onPress={() => setTab(entry.status)} />
          ))}
        </View>
      </View>

      {query.isLoading ? (
        <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.sm }}>
          {Array.from({ length: 4 }).map((_, i) => (
            <Shimmer key={i} height={90} radius={theme.radius.lg} />
          ))}
        </View>
      ) : query.isError ? (
        <ErrorState message="Could not load cases." onRetry={() => query.refetch()} />
      ) : (
        <ScrollView
          refreshControl={<RefreshControl refreshing={query.isRefetching} onRefresh={() => query.refetch()} tintColor={theme.colors.accent} />}
          contentContainerStyle={{ paddingHorizontal: theme.layout.gutter, paddingBottom: theme.spacing['3xl'], gap: theme.spacing.sm }}
        >
          {(query.data?.items.length ?? 0) === 0 ? (
            <EmptyState
              variant="inbox"
              title="No cases"
              description={
                perspective === 'lawyer'
                  ? 'Create a case for a customer you have an appointment relationship with.'
                  : "Your lawyer will create a case here once they're working on your matter."
              }
            />
          ) : (
            query.data?.items.map((item) => (
              <Card key={item.id} onPress={() => onOpen(item.id)}>
                <View style={{ gap: 4 }}>
                  <View style={{ flexDirection: 'row', justifyContent: 'space-between', gap: theme.spacing.sm }}>
                    <Text variant="bodyMedium" style={{ flex: 1 }}>
                      {item.title}
                    </Text>
                    <Badge label={item.status} tone={statusTone(item.status)} />
                  </View>
                  <Text variant="caption" color="secondary">
                    {perspective === 'lawyer' ? item.otherPartyName : `Adv. ${item.otherPartyName}`} · {item.caseType}
                  </Text>
                  {item.upcomingAction ? (
                    <Text variant="caption" color="accent">
                      Next {item.upcomingAction.kind.toLowerCase()}: {new Date(item.upcomingAction.when).toLocaleDateString()}
                    </Text>
                  ) : null}
                  <Text variant="label" color="tertiary">
                    {item.daysSinceLastUpdate == null ? 'No updates yet' : `Last update ${item.daysSinceLastUpdate}d ago`}
                  </Text>
                </View>
              </Card>
            ))
          )}
        </ScrollView>
      )}
    </Screen>
  );
}
