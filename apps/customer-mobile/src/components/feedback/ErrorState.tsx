import React from 'react';
import { View, type StyleProp, type ViewStyle } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type ErrorStateProps = {
  title?: string;
  message?: string;
  onRetry?: () => void;
  style?: StyleProp<ViewStyle>;
};

export function ErrorState({
  title = 'Something went wrong',
  message = 'Please try again in a moment.',
  onRetry,
  style,
}: ErrorStateProps) {
  const theme = useTheme();

  return (
    <View
      style={[
        {
          alignItems: 'center',
          justifyContent: 'center',
          padding: theme.spacing.xl,
          gap: theme.spacing.sm,
        },
        style,
      ]}
    >
      <View
        style={{
          width: 64,
          height: 64,
          borderRadius: theme.radius.full,
          backgroundColor: theme.colors.dangerMuted,
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: theme.spacing.xs,
        }}
      >
        <Icon name="alert" size={28} color={theme.colors.danger} />
      </View>
      <Text variant="headline" align="center">
        {title}
      </Text>
      <Text variant="body" color="secondary" align="center">
        {message}
      </Text>
      {onRetry ? (
        <View style={{ marginTop: theme.spacing.md, alignSelf: 'stretch', maxWidth: 280 }}>
          <Button title="Try again" onPress={onRetry} />
        </View>
      ) : null}
    </View>
  );
}
