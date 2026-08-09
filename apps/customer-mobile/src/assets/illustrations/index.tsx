import React from 'react';
import Svg, { Circle, Path, Rect } from 'react-native-svg';

import { useTheme } from '@/theme/ThemeProvider';

type IllustrationProps = {
  size?: number;
};

export function EmptySearchIllustration({ size = 160 }: IllustrationProps) {
  const { colors } = useTheme();
  return (
    <Svg width={size} height={size} viewBox="0 0 160 160">
      <Circle cx="80" cy="80" r="72" fill={colors.accentSoft} />
      <Circle cx="74" cy="72" r="28" stroke={colors.accent} strokeWidth="3" fill={colors.surface} />
      <Path d="M94 92l22 22" stroke={colors.accent} strokeWidth="3" strokeLinecap="round" />
      <Path d="M64 72h20M74 62v20" stroke={colors.textTertiary} strokeWidth="2.5" strokeLinecap="round" />
    </Svg>
  );
}

export function EmptyInboxIllustration({ size = 160 }: IllustrationProps) {
  const { colors } = useTheme();
  return (
    <Svg width={size} height={size} viewBox="0 0 160 160">
      <Circle cx="80" cy="80" r="72" fill={colors.surfaceMuted} />
      <Rect x="42" y="52" width="76" height="56" rx="14" fill={colors.surface} stroke={colors.borderStrong} strokeWidth="2" />
      <Path d="M42 70h76" stroke={colors.borderStrong} strokeWidth="2" />
      <Path d="M62 96h36" stroke={colors.textTertiary} strokeWidth="3" strokeLinecap="round" />
    </Svg>
  );
}

export function EmptyGenericIllustration({ size = 160 }: IllustrationProps) {
  const { colors } = useTheme();
  return (
    <Svg width={size} height={size} viewBox="0 0 160 160">
      <Circle cx="80" cy="80" r="72" fill={colors.accentSoft} />
      <Rect x="48" y="48" width="64" height="64" rx="16" fill={colors.surface} stroke={colors.accent} strokeWidth="2.5" />
      <Path d="M64 80h32M80 64v32" stroke={colors.accent} strokeWidth="3" strokeLinecap="round" />
    </Svg>
  );
}

export function OfflineIllustration({ size = 120 }: IllustrationProps) {
  const { colors } = useTheme();
  return (
    <Svg width={size} height={size} viewBox="0 0 120 120">
      <Circle cx="60" cy="60" r="52" fill={colors.warningMuted} />
      <Path
        d="M30 48c10-9 22-12 34-11M42 62c7-5 15-7 22-6M52 76c4-2 8-3 12-2"
        stroke={colors.warning}
        strokeWidth="3"
        strokeLinecap="round"
        fill="none"
      />
      <Path d="M40 40l40 40" stroke={colors.danger} strokeWidth="3" strokeLinecap="round" />
    </Svg>
  );
}
