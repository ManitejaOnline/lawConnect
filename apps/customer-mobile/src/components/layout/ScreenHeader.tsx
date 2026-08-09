import React from 'react';
import { Pressable, View, type StyleProp, type ViewStyle } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type ScreenHeaderProps = {
  title: string;
  subtitle?: string;
  onBack?: () => void;
  rightSlot?: React.ReactNode;
  style?: StyleProp<ViewStyle>;
};

export function ScreenHeader({ title, subtitle, onBack, rightSlot, style }: ScreenHeaderProps) {
  const theme = useTheme();

  return (
    <View
      style={[
        {
          flexDirection: 'row',
          alignItems: 'center',
          gap: theme.spacing.sm,
          marginBottom: theme.spacing.sm,
          minHeight: theme.touchTarget.min,
        },
        style,
      ]}
    >
      {onBack ? (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Go back"
          hitSlop={theme.hitSlop.md}
          onPress={onBack}
          style={{
            width: theme.touchTarget.min,
            height: theme.touchTarget.min,
            borderRadius: theme.radius.full,
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: theme.colors.surface,
            borderWidth: 1,
            borderColor: theme.colors.border,
          }}
        >
          <Icon name="chevron-left" size={20} color={theme.colors.textPrimary} />
        </Pressable>
      ) : null}
      <View style={{ flex: 1, gap: 2 }}>
        <Text variant="title">{title}</Text>
        {subtitle ? (
          <Text variant="caption" color="secondary">
            {subtitle}
          </Text>
        ) : null}
      </View>
      {rightSlot}
    </View>
  );
}
