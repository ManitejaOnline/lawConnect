import React from 'react';
import { View } from 'react-native';

import { Badge, type BadgeProps } from '@/components/ui/Badge';
import { Text } from '@/components/ui/Text';
import { Icon, type IconName } from '@/components/icons/Icon';
import { useTheme } from '@/theme/ThemeProvider';
import type { CaseTimelineEntry } from '@/types/case';

const KIND_ICON: Record<CaseTimelineEntry['kind'], IconName> = {
  CaseCreated: 'briefcase',
  Update: 'edit',
  Hearing: 'gavel',
  Milestone: 'flag',
};

function statusTone(status: string | null): NonNullable<BadgeProps['tone']> {
  switch (status) {
    case 'Completed':
    case 'Won':
    case 'Closed':
    case 'Settled':
      return 'success';
    case 'Overdue':
    case 'Lost':
    case 'Cancelled':
      return 'danger';
    case 'Scheduled':
    case 'Pending':
    case 'InProgress':
    case 'Active':
      return 'accent';
    case 'Adjourned':
    case 'OnHold':
      return 'warning';
    default:
      return 'neutral';
  }
}

export function CaseTimeline({ entries }: { entries: CaseTimelineEntry[] }) {
  const theme = useTheme();

  return (
    <View style={{ gap: 0 }}>
      {entries.map((entry, index) => (
        <View key={`${entry.kind}-${entry.entityId}`} style={{ flexDirection: 'row', gap: theme.spacing.sm }}>
          <View style={{ alignItems: 'center', width: 28 }}>
            <View
              style={{
                width: 28,
                height: 28,
                borderRadius: 14,
                backgroundColor: theme.colors.surfaceMuted,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Icon name={KIND_ICON[entry.kind]} size={14} color={theme.colors.textSecondary} />
            </View>
            {index < entries.length - 1 ? (
              <View style={{ width: 2, flex: 1, backgroundColor: theme.colors.border, marginVertical: 2 }} />
            ) : null}
          </View>
          <View style={{ flex: 1, paddingBottom: theme.spacing.md, gap: 2 }}>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: theme.spacing.xs }}>
              <Text variant="bodyMedium" style={{ flex: 1 }}>
                {entry.title}
              </Text>
              {entry.status ? <Badge label={entry.status} tone={statusTone(entry.status)} /> : null}
            </View>
            {entry.description ? (
              <Text variant="caption" color="secondary">
                {entry.description}
              </Text>
            ) : null}
            <Text variant="label" color="tertiary">
              {new Date(entry.occurredAtUtc).toLocaleString()}
            </Text>
          </View>
        </View>
      ))}
    </View>
  );
}
