import React, { createContext, useContext, useMemo } from 'react';
import { useColorScheme } from 'react-native';

import { useThemeStore } from '@/store/themeStore';

import {
  animation,
  elevation,
  hitSlop,
  layout,
  radius,
  resolveColors,
  resolveScheme,
  spacing,
  touchTarget,
  typography,
  type ColorTokens,
  type ThemeMode,
} from './index';

export type AppTheme = {
  mode: ThemeMode;
  scheme: 'light' | 'dark';
  colors: ColorTokens;
  spacing: typeof spacing;
  radius: typeof radius;
  typography: typeof typography;
  elevation: typeof elevation;
  animation: typeof animation;
  hitSlop: typeof hitSlop;
  touchTarget: typeof touchTarget;
  layout: typeof layout;
  setMode: (mode: ThemeMode) => void;
};

const ThemeContext = createContext<AppTheme | null>(null);

type ThemeProviderProps = {
  children: React.ReactNode;
};

export function ThemeProvider({ children }: ThemeProviderProps) {
  const mode = useThemeStore((s) => s.mode);
  const setMode = useThemeStore((s) => s.setMode);
  const systemScheme = useColorScheme();
  const systemDark = systemScheme === 'dark';

  const value = useMemo<AppTheme>(() => {
    const scheme = resolveScheme(mode, systemDark);
    return {
      mode,
      scheme,
      colors: resolveColors(mode, systemDark),
      spacing,
      radius,
      typography,
      elevation,
      animation,
      hitSlop,
      touchTarget,
      layout,
      setMode,
    };
  }, [mode, setMode, systemDark]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): AppTheme {
  const ctx = useContext(ThemeContext);
  if (!ctx) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return ctx;
}
