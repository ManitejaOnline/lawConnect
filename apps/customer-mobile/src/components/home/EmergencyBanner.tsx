import React from 'react';
import { Pressable, View, type StyleProp, type ViewStyle } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type EmergencyBannerProps = {
  onPress?: () => void;
  style?: StyleProp<ViewStyle>;
  /** Red attention dot — only when the customer has an actionable SOS. */
  showDot?: boolean;
};

export function EmergencyBanner({ onPress, style, showDot = false }: EmergencyBannerProps) {
  const theme = useTheme();

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={showDot ? 'Emergency SOS, active emergency' : 'Emergency SOS'}
      accessibilityHint="Opens emergency help options"
      onPress={onPress}
      style={({ pressed }) => [
        {
          borderRadius: theme.radius.lg,
          paddingVertical: theme.spacing.sm,
          paddingHorizontal: theme.spacing.md,
          backgroundColor: theme.colors.surface,
          borderWidth: 1,
          borderColor: theme.colors.border,
          flexDirection: 'row',
          alignItems: 'center',
          gap: theme.spacing.sm,
          opacity: pressed ? 0.94 : 1,
          ...theme.elevation.sm,
        },
        style,
      ]}
    >
      <View style={{ flex: 1, gap: 2 }}>
        <Text variant="bodyMedium">Need Immediate Help?</Text>
        <Text variant="caption" color="secondary">
          Tap for Emergency SOS
        </Text>
      </View>
      <View
        style={{
          width: 48,
          height: 48,
          borderRadius: 24,
          backgroundColor: theme.colors.emergency,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Text variant="label" color="inverse" style={{ fontSize: 12, letterSpacing: 0.6 }}>
          SOS
        </Text>
        {showDot ? (
          <View
            style={{
              position: 'absolute',
              top: 2,
              right: 2,
              width: 8,
              height: 8,
              borderRadius: 4,
              backgroundColor: theme.colors.surface,
              borderWidth: 2,
              borderColor: theme.colors.emergency,
            }}
          />
        ) : null}
      </View>
    </Pressable>
  );
}
