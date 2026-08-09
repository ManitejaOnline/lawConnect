import React from 'react';
import { View } from 'react-native';
import Svg, { Circle, Path } from 'react-native-svg';

import { useTheme } from '@/theme/ThemeProvider';
import { palette } from '@/theme';

/** Abstract map-like backdrop — no external map SDK required. */
export function MapBackdrop({ children }: { children?: React.ReactNode }) {
  const theme = useTheme();

  return (
    <View
      style={{
        flex: 1,
        backgroundColor: palette.ink900,
        overflow: 'hidden',
      }}
    >
      <Svg width="100%" height="100%" style={{ position: 'absolute' }} viewBox="0 0 390 844">
        <Path
          d="M0 180 C80 140 140 220 220 190 C300 160 340 210 390 170 L390 844 L0 844 Z"
          fill={palette.ink800}
          opacity={0.9}
        />
        <Path
          d="M0 320 C90 280 150 360 240 330 C320 305 350 360 390 340 L390 844 L0 844 Z"
          fill={palette.ink700}
          opacity={0.55}
        />
        <Path d="M40 240 H350" stroke={palette.ink500} strokeWidth="1.5" opacity={0.35} />
        <Path d="M60 400 H320" stroke={palette.ink500} strokeWidth="1.5" opacity={0.25} />
        <Path d="M120 120 V520" stroke={palette.ink500} strokeWidth="1.5" opacity={0.25} />
        <Path d="M260 80 V560" stroke={palette.ink500} strokeWidth="1.5" opacity={0.2} />
        <Circle cx="195" cy="360" r="8" fill={theme.colors.danger} />
        <Circle cx="195" cy="360" r="22" stroke={theme.colors.danger} strokeWidth="2" fill="none" opacity={0.5} />
      </Svg>
      {children}
    </View>
  );
}
