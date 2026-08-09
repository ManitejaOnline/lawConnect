import React from 'react';
import { ActivityIndicator, View, type StyleProp, type ViewStyle } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type LoadingStateProps = {
  label?: string;
  style?: StyleProp<ViewStyle>;
  fullScreen?: boolean;
};

export function LoadingState({ label = 'Loading', style, fullScreen = false }: LoadingStateProps) {
  const theme = useTheme();

  return (
    <View
      style={[
        {
          alignItems: 'center',
          justifyContent: 'center',
          gap: theme.spacing.sm,
          padding: theme.spacing.xl,
          flex: fullScreen ? 1 : undefined,
        },
        style,
      ]}
    >
      <ActivityIndicator size="large" color={theme.colors.accent} />
      <Text variant="bodyMedium" color="secondary">
        {label}
      </Text>
    </View>
  );
}
