import React from 'react';
import { View } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';
import type { TrustScoreBreakdown } from '@/types/api';

const COMPONENTS: { key: keyof TrustScoreBreakdown; label: string; max: number }[] = [
  { key: 'clientRating', label: 'Client Ratings', max: 25 },
  { key: 'appointmentReliability', label: 'Appointment Reliability', max: 20 },
  { key: 'responsiveness', label: 'Responsiveness', max: 15 },
  { key: 'caseUpdates', label: 'Case Updates', max: 15 },
  { key: 'caseManagement', label: 'Case Management', max: 15 },
  { key: 'verifiedComplaints', label: 'Verified Complaints', max: 10 },
];

export function TrustScoreBreakdownList({ breakdown }: { breakdown: TrustScoreBreakdown }) {
  const theme = useTheme();

  return (
    <View style={{ gap: theme.spacing.sm }}>
      {COMPONENTS.map(({ key, label, max }) => {
        const value = breakdown[key];
        const pct = Math.max(0, Math.min(1, value / max));
        return (
          <View key={key} style={{ gap: 4 }}>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
              <Text variant="body">{label}</Text>
              <Text variant="caption" color="secondary">
                {value.toFixed(1)} / {max}
              </Text>
            </View>
            <View
              style={{
                height: 6,
                borderRadius: 3,
                backgroundColor: theme.colors.surfaceMuted,
                overflow: 'hidden',
              }}
            >
              <View
                style={{
                  width: `${pct * 100}%`,
                  height: '100%',
                  backgroundColor: theme.colors.accent,
                  borderRadius: 3,
                }}
              />
            </View>
          </View>
        );
      })}
    </View>
  );
}
