import React, { useMemo } from 'react';
import { Image, View, type StyleProp, type ViewStyle } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type AvatarProps = {
  uri?: string | null;
  name?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  style?: StyleProp<ViewStyle>;
};

const sizeMap = {
  sm: 32,
  md: 40,
  lg: 56,
  xl: 80,
} as const;

function initials(name?: string) {
  if (!name?.trim()) return '?';
  const parts = name.trim().split(/\s+/).slice(0, 2);
  return parts.map((p) => p[0]?.toUpperCase() ?? '').join('');
}

export function Avatar({ uri, name, size = 'md', style }: AvatarProps) {
  const theme = useTheme();
  const dim = sizeMap[size];
  const fontSize = dim * 0.36;

  const bg = useMemo(() => theme.colors.accentMuted, [theme.colors.accentMuted]);

  return (
    <View
      style={[
        {
          width: dim,
          height: dim,
          borderRadius: theme.radius.full,
          backgroundColor: bg,
          alignItems: 'center',
          justifyContent: 'center',
          overflow: 'hidden',
          borderWidth: 1,
          borderColor: theme.colors.border,
        },
        style,
      ]}
    >
      {uri ? (
        <Image source={{ uri }} style={{ width: dim, height: dim }} accessibilityIgnoresInvertColors />
      ) : (
        <Text
          variant="label"
          style={{
            fontSize,
            lineHeight: fontSize * 1.2,
            color: theme.colors.accent,
            fontFamily: theme.typography.fontFamily.display,
          }}
        >
          {initials(name)}
        </Text>
      )}
    </View>
  );
}
