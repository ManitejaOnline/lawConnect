import React from 'react';
import { Pressable, View } from 'react-native';

import { Icon, type IconName } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export function ProfileMenuItem({
  icon,
  label,
  onPress,
  danger = false,
}: {
  icon: IconName;
  label: string;
  onPress?: () => void;
  danger?: boolean;
}) {
  const theme = useTheme();
  const color = danger ? theme.colors.danger : theme.colors.textPrimary;

  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      style={({ pressed }) => ({
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.sm,
        paddingVertical: 12,
        paddingHorizontal: theme.spacing.sm,
        borderBottomWidth: 1,
        borderBottomColor: theme.colors.border,
        backgroundColor: pressed ? theme.colors.surfaceMuted : theme.colors.surface,
      })}
    >
      <Icon name={icon} size={18} color={color} />
      <Text variant="body" style={{ flex: 1, color }}>
        {label}
      </Text>
      {!danger ? <Icon name="chevron-right" size={16} color={theme.colors.textTertiary} /> : null}
    </Pressable>
  );
}
