import React from 'react';
import { Pressable, type StyleProp, type ViewStyle } from 'react-native';

import { Icon, type IconName } from '@/components/icons/Icon';
import { useTheme } from '@/theme/ThemeProvider';

export type IconButtonProps = {
  name: IconName;
  onPress?: () => void;
  accessibilityLabel: string;
  color?: string;
  size?: number;
  style?: StyleProp<ViewStyle>;
};

export function IconButton({ name, onPress, accessibilityLabel, color, size = 20, style }: IconButtonProps) {
  const theme = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      hitSlop={theme.hitSlop.md}
      onPress={onPress}
      style={[
        {
          width: theme.touchTarget.min,
          height: theme.touchTarget.min,
          borderRadius: theme.radius.md,
          alignItems: 'center',
          justifyContent: 'center',
          borderWidth: 1,
          borderColor: theme.colors.border,
          backgroundColor: theme.colors.surface,
        },
        style,
      ]}
    >
      <Icon name={name} size={size} color={color ?? theme.colors.textPrimary} />
    </Pressable>
  );
}
