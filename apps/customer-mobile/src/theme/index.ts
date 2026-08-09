import { darkColors, lightColors, type ColorTokens, type ThemeMode } from './tokens';

export { animation, elevation, hitSlop, layout, palette, radius, spacing, touchTarget, typography } from './tokens';
export type { ColorTokens, ThemeMode } from './tokens';

export function resolveColors(mode: ThemeMode, systemDark: boolean): ColorTokens {
  const isDark = mode === 'dark' || (mode === 'system' && systemDark);
  return isDark ? darkColors : lightColors;
}

export function resolveScheme(mode: ThemeMode, systemDark: boolean): 'light' | 'dark' {
  if (mode === 'system') {
    return systemDark ? 'dark' : 'light';
  }
  return mode;
}
