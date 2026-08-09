import React from 'react';
import { View } from 'react-native';

import { Shimmer } from '@/components/feedback/Shimmer';
import { useTheme } from '@/theme/ThemeProvider';

export function HomeSkeleton() {
  const theme = useTheme();

  return (
    <View style={{ gap: theme.spacing.md }} accessibilityLabel="Loading home">
      <Shimmer height={120} radius={theme.radius.xl} />
      <Shimmer height={72} radius={theme.radius.xl} />
      <View style={{ flexDirection: 'row', gap: theme.spacing.sm }}>
        {Array.from({ length: 3 }).map((_, i) => (
          <Shimmer key={i} width={108} height={112} radius={theme.radius.lg} />
        ))}
      </View>
      {Array.from({ length: 3 }).map((_, i) => (
        <View
          key={`card-${i}`}
          style={{
            padding: theme.spacing.md,
            borderRadius: theme.radius.lg,
            backgroundColor: theme.colors.surface,
            borderWidth: 1,
            borderColor: theme.colors.border,
            gap: theme.spacing.sm,
          }}
        >
          <View style={{ flexDirection: 'row', gap: theme.spacing.sm }}>
            <Shimmer width={56} height={56} radius={theme.radius.full} />
            <View style={{ flex: 1, gap: theme.spacing.xs }}>
              <Shimmer width="55%" height={14} />
              <Shimmer width="80%" height={12} />
              <Shimmer width="40%" height={12} />
            </View>
          </View>
        </View>
      ))}
    </View>
  );
}

export function LawyerListSkeleton({ count = 4 }: { count?: number }) {
  const theme = useTheme();
  return (
    <View style={{ gap: theme.spacing.sm }}>
      {Array.from({ length: count }).map((_, i) => (
        <View
          key={i}
          style={{
            padding: theme.spacing.md,
            borderRadius: theme.radius.lg,
            backgroundColor: theme.colors.surface,
            borderWidth: 1,
            borderColor: theme.colors.border,
            gap: theme.spacing.sm,
          }}
        >
          <View style={{ flexDirection: 'row', gap: theme.spacing.sm }}>
            <Shimmer width={56} height={56} radius={theme.radius.full} />
            <View style={{ flex: 1, gap: theme.spacing.xs }}>
              <Shimmer width="50%" height={14} />
              <Shimmer width="70%" height={12} />
              <Shimmer width="35%" height={12} />
            </View>
          </View>
        </View>
      ))}
    </View>
  );
}
