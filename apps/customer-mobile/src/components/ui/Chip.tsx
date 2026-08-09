import React from 'react';
import { Pressable, View, type StyleProp, type ViewStyle } from 'react-native';

import { Icon, type IconName } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type ChipProps = {
  label: string;
  selected?: boolean;
  onPress?: () => void;
  icon?: IconName;
  style?: StyleProp<ViewStyle>;
};

export function Chip({ label, selected = false, onPress, icon, style }: ChipProps) {
  const theme = useTheme();

  const content = (
    <View
      style={[
        {
          minHeight: 36,
          paddingHorizontal: theme.spacing.sm,
          borderRadius: theme.radius.full,
          borderWidth: 1,
          borderColor: selected ? theme.colors.primary : theme.colors.border,
          backgroundColor: selected ? theme.colors.primaryMuted : theme.colors.surface,
          flexDirection: 'row',
          alignItems: 'center',
          gap: theme.spacing.xxs,
        },
        style,
      ]}
    >
      {icon ? (
        <Icon name={icon} size={14} color={selected ? theme.colors.primary : theme.colors.textSecondary} />
      ) : null}
      <Text variant="caption" style={{ color: selected ? theme.colors.primary : theme.colors.textPrimary }}>
        {label}
      </Text>
    </View>
  );

  if (onPress) {
    return (
      <Pressable accessibilityRole="button" accessibilityState={{ selected }} onPress={onPress} hitSlop={theme.hitSlop.sm}>
        {content}
      </Pressable>
    );
  }

  return content;
}
