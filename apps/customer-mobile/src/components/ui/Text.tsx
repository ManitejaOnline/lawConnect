import React from 'react';
import { Text as RNText, type TextProps as RNTextProps, type TextStyle } from 'react-native';

import { useTheme } from '@/theme/ThemeProvider';

type TextVariant = 'display' | 'title' | 'headline' | 'body' | 'bodyMedium' | 'caption' | 'label';

export type AppTextProps = RNTextProps & {
  variant?: TextVariant;
  color?: 'primary' | 'secondary' | 'tertiary' | 'inverse' | 'link' | 'danger' | 'accent' | 'emergency';
  align?: TextStyle['textAlign'];
};

const variantMap: Record<
  TextVariant,
  { size: keyof ReturnType<typeof useTheme>['typography']['size']; family: keyof ReturnType<typeof useTheme>['typography']['fontFamily']; lh: keyof ReturnType<typeof useTheme>['typography']['lineHeight']; tracking?: number }
> = {
  display: { size: '3xl', family: 'displayBold', lh: '3xl', tracking: -0.3 },
  title: { size: '2xl', family: 'displayBold', lh: '2xl', tracking: -0.2 },
  headline: { size: 'xl', family: 'display', lh: 'xl', tracking: -0.1 },
  body: { size: 'md', family: 'body', lh: 'md' },
  bodyMedium: { size: 'md', family: 'bodyMedium', lh: 'md' },
  caption: { size: 'xs', family: 'body', lh: 'xs' },
  label: { size: 'xs', family: 'bodyMedium', lh: 'xs', tracking: 0.3 },
};

export function Text({
  variant = 'body',
  color = 'primary',
  align,
  style,
  children,
  ...rest
}: AppTextProps) {
  const theme = useTheme();
  const map = variantMap[variant];

  const colorValue = {
    primary: theme.colors.textPrimary,
    secondary: theme.colors.textSecondary,
    tertiary: theme.colors.textTertiary,
    inverse: theme.colors.textInverse,
    link: theme.colors.textLink,
    danger: theme.colors.danger,
    accent: theme.colors.accent,
    emergency: theme.colors.emergency,
  }[color];

  return (
    <RNText
      style={[
        {
          fontFamily: theme.typography.fontFamily[map.family],
          fontSize: theme.typography.size[map.size],
          lineHeight: theme.typography.lineHeight[map.lh],
          letterSpacing: map.tracking ?? theme.typography.letterSpacing.normal,
          color: colorValue,
          textAlign: align,
        },
        style,
      ]}
      {...rest}
    >
      {children}
    </RNText>
  );
}
