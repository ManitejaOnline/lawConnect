import React from 'react';
import { View } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

const STEPS = [
  { n: 1, label: 'Basic' },
  { n: 2, label: 'Professional' },
  { n: 3, label: 'Documents' },
  { n: 4, label: 'Profile' },
  { n: 5, label: 'Submit' },
] as const;

type Props = {
  current: number;
};

export function OnboardingProgress({ current }: Props) {
  const theme = useTheme();

  return (
    <View style={{ flexDirection: 'row', gap: theme.spacing.xs, marginBottom: theme.spacing.md }}>
      {STEPS.map((step) => {
        const active = step.n === current;
        const done = step.n < current;
        return (
          <View key={step.n} style={{ flex: 1, gap: 4 }}>
            <View
              style={{
                height: 4,
                borderRadius: theme.radius.full,
                backgroundColor: done || active ? theme.colors.accent : theme.colors.borderStrong,
              }}
            />
            <Text variant="label" color={active ? 'accent' : done ? 'primary' : 'tertiary'} numberOfLines={1}>
              {step.n} {step.label}
            </Text>
          </View>
        );
      })}
    </View>
  );
}
