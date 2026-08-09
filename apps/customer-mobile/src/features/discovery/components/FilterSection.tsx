import React from 'react';
import { Pressable, View } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type FilterSectionProps = {
  title: string;
  children: React.ReactNode;
};

export function FilterSection({ title, children }: FilterSectionProps) {
  const theme = useTheme();
  return (
    <View style={{ gap: theme.spacing.sm, marginBottom: theme.spacing.lg }}>
      <Text variant="label" color="secondary">
        {title}
      </Text>
      {children}
    </View>
  );
}

export type SelectableChipProps = {
  label: string;
  selected?: boolean;
  onPress?: () => void;
};

export function SelectableChip({ label, selected, onPress }: SelectableChipProps) {
  const theme = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: Boolean(selected) }}
      onPress={onPress}
      hitSlop={theme.hitSlop.sm}
      style={{
        minHeight: theme.touchTarget.min,
        paddingHorizontal: theme.spacing.sm,
        borderRadius: theme.radius.full,
        borderWidth: 1,
        borderColor: selected ? theme.colors.brand : theme.colors.border,
        backgroundColor: selected ? theme.colors.brand : theme.colors.surface,
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <Text variant="caption" style={{ color: selected ? theme.colors.textInverse : theme.colors.textPrimary }}>
        {label}
      </Text>
    </Pressable>
  );
}
