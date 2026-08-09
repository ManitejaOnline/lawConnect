import React from 'react';
import { Pressable, View, type StyleProp, type ViewStyle } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type SectionHeaderProps = {
  title: string;
  actionLabel?: string;
  onAction?: () => void;
  style?: StyleProp<ViewStyle>;
};

export function SectionHeader({ title, actionLabel = 'See all', onAction, style }: SectionHeaderProps) {
  const theme = useTheme();

  return (
    <View
      style={[
        {
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: theme.spacing.sm,
          minHeight: 36,
        },
        style,
      ]}
    >
      <Text variant="headline">{title}</Text>
      {onAction ? (
        <Pressable accessibilityRole="button" hitSlop={theme.hitSlop.md} onPress={onAction}>
          <Text variant="caption" color="link">
            {actionLabel}
          </Text>
        </Pressable>
      ) : null}
    </View>
  );
}
