import React from 'react';
import { View, type StyleProp, type ViewStyle } from 'react-native';

import { useTheme } from '@/theme/ThemeProvider';

export type BottomActionBarProps = {
  children: React.ReactNode;
  style?: StyleProp<ViewStyle>;
};

/** Compact sticky footer row (~60–72px with safe area handled by Screen). */
export function BottomActionBar({ children, style }: BottomActionBarProps) {
  const theme = useTheme();
  return (
    <View
      style={[
        {
          flexDirection: 'row',
          alignItems: 'center',
          gap: theme.spacing.xs,
          minHeight: 52,
        },
        style,
      ]}
    >
      {children}
    </View>
  );
}
