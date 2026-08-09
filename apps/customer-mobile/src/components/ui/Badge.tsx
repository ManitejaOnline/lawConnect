import React from 'react';
import { View, type StyleProp, type ViewStyle } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type BadgeProps = {
  label: string | number;
  tone?: 'neutral' | 'accent' | 'success' | 'warning' | 'danger' | 'emergency';
  style?: StyleProp<ViewStyle>;
};

export function Badge({ label, tone = 'neutral', style }: BadgeProps) {
  const theme = useTheme();

  const tones = {
    neutral: { bg: theme.colors.surfaceMuted, fg: theme.colors.textSecondary },
    accent: { bg: theme.colors.accentMuted, fg: theme.colors.accent },
    success: { bg: theme.colors.successMuted, fg: theme.colors.success },
    warning: { bg: theme.colors.warningMuted, fg: theme.colors.warning },
    danger: { bg: theme.colors.dangerMuted, fg: theme.colors.danger },
    emergency: { bg: theme.colors.emergencyMuted, fg: theme.colors.emergency },
  }[tone];

  return (
    <View
      style={[
        {
          alignSelf: 'flex-start',
          minHeight: 22,
          paddingHorizontal: theme.spacing.xs,
          paddingVertical: 2,
          borderRadius: theme.radius.full,
          backgroundColor: tones.bg,
          alignItems: 'center',
          justifyContent: 'center',
        },
        style,
      ]}
    >
      <Text variant="label" style={{ color: tones.fg }}>
        {label}
      </Text>
    </View>
  );
}
