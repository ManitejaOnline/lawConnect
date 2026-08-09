import React from 'react';
import { Pressable, View, type StyleProp, type ViewStyle } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';
import type { LawyerSearchItem } from '@/types/home';

export type FeaturedBannerProps = {
  lawyer?: LawyerSearchItem | null;
  onPress?: () => void;
  style?: StyleProp<ViewStyle>;
};

export function FeaturedBanner({ lawyer, onPress, style }: FeaturedBannerProps) {
  const theme = useTheme();
  const title = lawyer
    ? `${lawyer.firstName} ${lawyer.lastName}`
    : 'Verified lawyers near you';
  const subtitle = lawyer
    ? `${lawyer.practiceAreas?.[0] ?? 'General practice'} · ${lawyer.ratingAverage.toFixed(1)} ★`
    : 'Book consultations with trusted professionals';

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel="Featured lawyer"
      onPress={onPress}
      style={({ pressed }) => [
        {
          borderRadius: theme.radius.md,
          padding: theme.spacing.sm,
          backgroundColor: theme.colors.surface,
          borderWidth: 1,
          borderColor: theme.colors.border,
          borderLeftWidth: 3,
          borderLeftColor: theme.colors.primary,
          opacity: pressed ? 0.94 : 1,
        },
        style,
      ]}
    >
      <Text variant="label" color="accent">
        FEATURED
      </Text>
      <Text variant="headline" style={{ marginTop: 4 }}>
        {title}
      </Text>
      <Text variant="caption" color="secondary" style={{ marginTop: 2 }}>
        {subtitle}
      </Text>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: theme.spacing.xs }}>
        <Text variant="caption" color="link">
          View profile
        </Text>
        <Icon name="arrow-right" size={14} color={theme.colors.textLink} />
      </View>
    </Pressable>
  );
}
