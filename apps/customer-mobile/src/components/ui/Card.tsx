import React from 'react';
import { Pressable, StyleSheet, View, type StyleProp, type ViewStyle } from 'react-native';

import { useTheme } from '@/theme/ThemeProvider';

export type CardProps = {
  children: React.ReactNode;
  padded?: boolean;
  elevated?: boolean;
  onPress?: () => void;
  accessibilityLabel?: string;
  style?: StyleProp<ViewStyle>;
};

export function Card({ children, padded = true, elevated = true, onPress, accessibilityLabel, style }: CardProps) {
  const theme = useTheme();

  const body = (
    <View
      style={[
        {
          backgroundColor: theme.colors.surface,
          borderRadius: theme.radius.lg,
          borderWidth: StyleSheet.hairlineWidth,
          borderColor: theme.colors.border,
          padding: padded ? theme.spacing.md : 0,
          overflow: 'hidden',
        },
        elevated ? theme.elevation.sm : theme.elevation.none,
        style,
      ]}
    >
      {children}
    </View>
  );

  if (onPress) {
    return (
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={accessibilityLabel}
        onPress={onPress}
        style={({ pressed }) => [{ opacity: pressed ? 0.92 : 1 }]}
      >
        {body}
      </Pressable>
    );
  }

  return body;
}
