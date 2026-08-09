import React from 'react';
import { View } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export function BrandMark({ size = 64 }: { size?: number }) {
  const theme = useTheme();
  return (
    <View style={{ alignItems: 'center', gap: theme.spacing.sm }}>
      <View
        style={{
          width: size,
          height: size,
          borderRadius: size * 0.28,
          backgroundColor: theme.colors.primary,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Text variant="title" color="inverse" style={{ fontSize: size * 0.34, lineHeight: size * 0.4 }}>
          LC
        </Text>
      </View>
      <Text variant="title">LawConnect</Text>
    </View>
  );
}
