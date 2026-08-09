import React from 'react';
import { View, type StyleProp, type ViewStyle } from 'react-native';

import {
  EmptyGenericIllustration,
  EmptyInboxIllustration,
  EmptySearchIllustration,
  OfflineIllustration,
} from '@/assets/illustrations';
import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type EmptyStateProps = {
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
  variant?: 'generic' | 'search' | 'inbox' | 'offline';
  style?: StyleProp<ViewStyle>;
};

export function EmptyState({
  title,
  description,
  actionLabel,
  onAction,
  variant = 'generic',
  style,
}: EmptyStateProps) {
  const theme = useTheme();

  const Illustration =
    variant === 'search'
      ? EmptySearchIllustration
      : variant === 'inbox'
        ? EmptyInboxIllustration
        : variant === 'offline'
          ? OfflineIllustration
          : EmptyGenericIllustration;

  return (
    <View
      style={[
        {
          alignItems: 'center',
          justifyContent: 'center',
          paddingHorizontal: theme.spacing.md,
          paddingVertical: theme.spacing.lg,
          gap: theme.spacing.sm,
        },
        style,
      ]}
    >
      <Illustration />
      <Text variant="headline" align="center">
        {title}
      </Text>
      {description ? (
        <Text variant="body" color="secondary" align="center">
          {description}
        </Text>
      ) : null}
      {actionLabel && onAction ? (
        <View style={{ marginTop: theme.spacing.md, alignSelf: 'stretch', maxWidth: 280 }}>
          <Button title={actionLabel} onPress={onAction} />
        </View>
      ) : null}
    </View>
  );
}
