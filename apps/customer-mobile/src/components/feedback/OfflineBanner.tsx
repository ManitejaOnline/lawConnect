import React from 'react';
import { View } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useIsOffline } from '@/hooks/useNetworkStatus';
import { useTheme } from '@/theme/ThemeProvider';

export function OfflineBanner() {
  const offline = useIsOffline();
  const theme = useTheme();

  if (!offline) return null;

  return (
    <View
      style={{
        backgroundColor: theme.colors.warningMuted,
        paddingHorizontal: theme.spacing.md,
        paddingVertical: theme.spacing.xs,
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.xs,
      }}
    >
      <Icon name="wifi-off" size={16} color={theme.colors.warning} />
      <Text variant="caption" style={{ color: theme.colors.warning, flex: 1 }}>
        You are offline. Some actions may be unavailable.
      </Text>
    </View>
  );
}
