import React from 'react';
import { View } from 'react-native';
import { useRoute, useNavigation, type RouteProp } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Badge, type BadgeProps } from '@/components/ui/Badge';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { CaseTimeline } from '@/components/case/CaseTimeline';
import { useAdminCaseQuery } from '@/features/admin/hooks/useAdminQueries';
import type { AdminCasesStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

type Route = RouteProp<AdminCasesStackParamList, 'AdminCaseDetail'>;
type Nav = NativeStackNavigationProp<AdminCasesStackParamList, 'AdminCaseDetail'>;

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <View style={{ gap: 2 }}>
      <Text variant="label" color="secondary">{label}</Text>
      <Text variant="body">{value ?? '—'}</Text>
    </View>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  const theme = useTheme();
  return (
    <View style={{ borderWidth: 1, borderColor: theme.colors.border, borderRadius: theme.radius.lg, padding: theme.spacing.md, gap: theme.spacing.sm, backgroundColor: theme.colors.surface }}>
      <Text variant="bodyMedium">{title}</Text>
      {children}
    </View>
  );
}

function statusTone(status: string): NonNullable<BadgeProps['tone']> {
  switch (status) {
    case 'Won':
    case 'Settled':
    case 'Closed':
    case 'Completed':
      return 'success';
    case 'Lost':
    case 'Cancelled':
    case 'Overdue':
      return 'danger';
    case 'OnHold':
    case 'Adjourned':
      return 'warning';
    case 'Active':
    case 'Scheduled':
      return 'accent';
    default:
      return 'neutral';
  }
}

export function AdminCaseDetailScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const { params } = useRoute<Route>();
  const query = useAdminCaseQuery(params.caseId);

  if (query.isLoading) {
    return <LoadingState fullScreen label="Loading case" />;
  }
  if (query.isError || !query.data) {
    return <ErrorState message="Could not load this case." onRetry={() => query.refetch()} />;
  }

  const { case: c, upcomingAction, activitySignals, timeline, hearings, milestones, updates } = query.data;

  return (
    <Screen edges={['top', 'left', 'right']} scroll padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.md, paddingBottom: theme.spacing['3xl'] }}>
        <ScreenHeader title={c.title} onBack={() => navigation.goBack()} />

        <View style={{ flexDirection: 'row', gap: theme.spacing.xs, flexWrap: 'wrap' }}>
          <Badge label={c.status} tone={statusTone(c.status)} />
          <Badge label={c.priority} tone="neutral" />
        </View>

        <Section title="Parties">
          <Field label="Lawyer" value={c.lawyerName} />
          <Field label="Customer" value={c.customerName} />
          <Field label="Case type" value={c.caseType} />
          <Field label="Court" value={c.courtName} />
        </Section>

        <Section title="Details">
          <Field label="Case number" value={c.caseNumber} />
          <Field label="Description" value={c.description} />
          <Field label="Start date" value={new Date(c.startDate).toLocaleDateString()} />
          <Field label="Closed date" value={c.closedDate ? new Date(c.closedDate).toLocaleDateString() : null} />
        </Section>

        {upcomingAction ? (
          <Section title={`Next ${upcomingAction.kind}`}>
            <Field label="What" value={upcomingAction.title} />
            <Field label="When" value={new Date(upcomingAction.when).toLocaleString()} />
            <Field label="Status" value={upcomingAction.status} />
          </Section>
        ) : null}

        <Section title="Activity signals">
          <Field label="Last update" value={activitySignals.daysSinceLastUpdate == null ? 'never' : `${activitySignals.daysSinceLastUpdate}d ago`} />
          <Field label="Last activity" value={`${activitySignals.daysSinceLastActivity}d ago`} />
          <Field label="Overdue milestones" value={activitySignals.overdueMilestoneCount} />
          <Field label="Adjourned hearings" value={activitySignals.missedHearingCount} />
          <Field label="Pending actions" value={activitySignals.pendingActionsCount} />
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm }}>
            <Field
              label="Last meaningful lawyer activity"
              value={activitySignals.daysSinceLastMeaningfulLawyerActivity == null ? 'never' : `${activitySignals.daysSinceLastMeaningfulLawyerActivity}d ago`}
            />
            {activitySignals.stallingStatus !== 'None' ? (
              <Badge
                label={activitySignals.stallingStatus === 'SevereStalling' ? 'Severely stalling' : activitySignals.stallingStatus}
                tone={activitySignals.stallingStatus === 'SevereStalling' ? 'danger' : activitySignals.stallingStatus === 'Stalling' ? 'warning' : 'neutral'}
              />
            ) : null}
          </View>
        </Section>

        <Section title={`Timeline (${timeline.length})`}>
          <CaseTimeline entries={timeline} />
        </Section>

        <Section title={`Hearings (${hearings.length})`}>
          {hearings.length === 0 ? <Text variant="caption" color="secondary">None.</Text> : hearings.map((h) => (
            <View key={h.id} style={{ flexDirection: 'row', justifyContent: 'space-between', gap: theme.spacing.sm }}>
              <Text variant="caption" style={{ flex: 1 }}>{h.hearingType ?? 'Hearing'} — {new Date(h.hearingDate).toLocaleDateString()}</Text>
              <Badge label={h.status} tone={statusTone(h.status)} />
            </View>
          ))}
        </Section>

        <Section title={`Milestones (${milestones.length})`}>
          {milestones.length === 0 ? <Text variant="caption" color="secondary">None.</Text> : milestones.map((m) => (
            <View key={m.id} style={{ flexDirection: 'row', justifyContent: 'space-between', gap: theme.spacing.sm }}>
              <Text variant="caption" style={{ flex: 1 }}>{m.title}</Text>
              <Badge label={m.status} tone={statusTone(m.status)} />
            </View>
          ))}
        </Section>

        <Section title={`Updates (${updates.length})`}>
          {updates.length === 0 ? <Text variant="caption" color="secondary">None.</Text> : updates.map((u) => (
            <View key={u.id} style={{ gap: 2 }}>
              <Text variant="caption">{u.title}</Text>
              <Text variant="label" color="tertiary">{new Date(u.createdAt).toLocaleString()}</Text>
            </View>
          ))}
        </Section>
      </View>
    </Screen>
  );
}
