/**
 * LawConnect design tokens — legal / fintech visual language.
 * Source: product UI reference (white, navy text, blue primary, red emergency only).
 */

export const palette = {
  navy: '#111827',
  gray600: '#687280',
  gray500: '#6B7280',
  gray400: '#9CA3AF',
  gray200: '#E5E7EB',
  gray100: '#F3F4F6',
  gray50: '#F9FAFB',

  blue700: '#1558B0',
  blue600: '#1A73E8',
  blue100: '#E8F1FC',
  blue50: '#F3F8FE',

  red600: '#E53935',
  red100: '#FDECEC',

  green600: '#16A34A',
  green100: '#DCFCE7',

  amber500: '#F59E0B',
  amber100: '#FEF3C7',

  white: '#FFFFFF',
  black: '#000000',
  transparent: 'transparent',

  /** @deprecated Use navy / gray tokens. Kept for emergency screens that still import palette.ink*. */
  ink950: '#111827',
  ink900: '#111827',
  ink800: '#1F2937',
  ink700: '#374151',
  ink500: '#687280',
  ink400: '#9CA3AF',
  ink300: '#D1D5DB',
  ink200: '#E5E7EB',
  ink100: '#F3F4F6',
  ink50: '#F9FAFB',
  teal700: '#1558B0',
  teal600: '#1A73E8',
  teal500: '#1A73E8',
  teal400: '#4C8DF0',
  teal100: '#E8F1FC',
  teal50: '#F3F8FE',
} as const;

export type ColorTokens = {
  background: string;
  backgroundElevated: string;
  surface: string;
  surfaceMuted: string;
  surfaceInverse: string;
  text: string;
  textPrimary: string;
  textSecondary: string;
  textTertiary: string;
  textInverse: string;
  textLink: string;
  border: string;
  borderStrong: string;
  divider: string;
  brand: string;
  brandMuted: string;
  primary: string;
  primaryMuted: string;
  accent: string;
  accentMuted: string;
  accentSoft: string;
  success: string;
  successMuted: string;
  warning: string;
  warningMuted: string;
  error: string;
  danger: string;
  dangerMuted: string;
  emergency: string;
  emergencyMuted: string;
  overlay: string;
  skeleton: string;
  skeletonHighlight: string;
  tabBar: string;
  tabBarBorder: string;
  tabInactive: string;
  tabActive: string;
  rating: string;
  focusRing: string;
};

export const lightColors: ColorTokens = {
  background: palette.white,
  backgroundElevated: palette.white,
  surface: palette.white,
  surfaceMuted: palette.gray100,
  surfaceInverse: palette.navy,

  text: palette.navy,
  textPrimary: palette.navy,
  textSecondary: palette.gray600,
  textTertiary: palette.gray400,
  textInverse: palette.white,
  textLink: palette.blue600,

  border: palette.gray200,
  borderStrong: '#D1D5DB',
  divider: palette.gray200,

  brand: palette.navy,
  brandMuted: palette.gray600,
  primary: palette.blue600,
  primaryMuted: palette.blue100,
  accent: palette.blue600,
  accentMuted: palette.blue100,
  accentSoft: palette.blue50,

  success: palette.green600,
  successMuted: palette.green100,
  warning: palette.amber500,
  warningMuted: palette.amber100,
  error: palette.red600,
  danger: palette.red600,
  dangerMuted: palette.red100,
  emergency: palette.red600,
  emergencyMuted: palette.red100,

  overlay: 'rgba(17, 24, 39, 0.45)',
  skeleton: palette.gray200,
  skeletonHighlight: palette.gray100,

  tabBar: palette.white,
  tabBarBorder: palette.gray200,
  tabInactive: palette.gray400,
  tabActive: palette.blue600,

  rating: palette.amber500,
  focusRing: palette.blue600,
};

export const darkColors: ColorTokens = {
  background: '#0B1220',
  backgroundElevated: '#111827',
  surface: '#151C2C',
  surfaceMuted: '#1F2937',
  surfaceInverse: palette.gray50,

  text: '#F9FAFB',
  textPrimary: '#F9FAFB',
  textSecondary: '#9CA3AF',
  textTertiary: '#6B7280',
  textInverse: palette.navy,
  textLink: '#60A5FA',

  border: '#243044',
  borderStrong: '#334155',
  divider: '#243044',

  brand: '#F9FAFB',
  brandMuted: '#D1D5DB',
  primary: '#3B82F6',
  primaryMuted: '#1E3A5F',
  accent: '#3B82F6',
  accentMuted: '#1E3A5F',
  accentSoft: '#172554',

  success: '#22C55E',
  successMuted: '#14532D',
  warning: palette.amber500,
  warningMuted: '#3A2A10',
  error: '#F87171',
  danger: '#F87171',
  dangerMuted: '#3F1515',
  emergency: palette.red600,
  emergencyMuted: '#3F1515',

  overlay: 'rgba(0, 0, 0, 0.64)',
  skeleton: '#1F2937',
  skeletonHighlight: '#243044',

  tabBar: '#111827',
  tabBarBorder: '#243044',
  tabInactive: '#6B7280',
  tabActive: '#60A5FA',

  rating: palette.amber500,
  focusRing: '#60A5FA',
};

export const spacing = {
  none: 0,
  xxs: 4,
  xs: 8,
  sm: 12,
  md: 16,
  lg: 20,
  xl: 24,
  '2xl': 32,
  '3xl': 40,
  '4xl': 48,
  '5xl': 64,
} as const;

export const radius = {
  none: 0,
  sm: 8,
  md: 10,
  lg: 12,
  xl: 16,
  '2xl': 20,
  full: 9999,
} as const;

export const typography = {
  fontFamily: {
    display: 'PlusJakartaSans_600SemiBold',
    displayBold: 'PlusJakartaSans_700Bold',
    displayMedium: 'PlusJakartaSans_500Medium',
    body: 'DMSans_400Regular',
    bodyMedium: 'DMSans_500Medium',
    bodyBold: 'DMSans_700Bold',
  },
  size: {
    xs: 12,
    sm: 13,
    md: 14,
    lg: 16,
    xl: 18,
    '2xl': 22,
    '3xl': 24,
    '4xl': 24,
  },
  lineHeight: {
    xs: 16,
    sm: 18,
    md: 20,
    lg: 22,
    xl: 24,
    '2xl': 28,
    '3xl': 30,
    '4xl': 30,
  },
  letterSpacing: {
    tight: -0.2,
    normal: 0,
    wide: 0.2,
  },
} as const;

export const elevation = {
  none: {
    shadowColor: palette.black,
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0,
    shadowRadius: 0,
    elevation: 0,
  },
  sm: {
    shadowColor: '#111827',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06,
    shadowRadius: 4,
    elevation: 1,
  },
  md: {
    shadowColor: '#111827',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.08,
    shadowRadius: 10,
    elevation: 3,
  },
  lg: {
    shadowColor: '#111827',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.1,
    shadowRadius: 16,
    elevation: 6,
  },
} as const;

export const animation = {
  duration: {
    fast: 160,
    normal: 240,
    slow: 360,
    entrance: 480,
  },
  easing: {
    standard: 'cubic-bezier(0.2, 0.0, 0, 1.0)',
    emphasized: 'cubic-bezier(0.2, 0.0, 0, 1.0)',
    exit: 'cubic-bezier(0.4, 0.0, 1, 1)',
  },
  spring: {
    damping: 18,
    stiffness: 220,
    mass: 0.9,
  },
} as const;

export const hitSlop = {
  sm: { top: 8, bottom: 8, left: 8, right: 8 },
  md: { top: 12, bottom: 12, left: 12, right: 12 },
  lg: { top: 16, bottom: 16, left: 16, right: 16 },
} as const;

export const touchTarget = {
  min: 40,
  comfortable: 44,
} as const;

export const layout = {
  maxContentWidth: 720,
  gutter: spacing.md,
  gutterTablet: spacing.xl,
  tabBarHeight: 56,
} as const;

export type ThemeMode = 'light' | 'dark' | 'system';
