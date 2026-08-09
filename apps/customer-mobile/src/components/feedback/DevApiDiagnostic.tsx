import React, { useEffect, useState } from 'react';
import { View } from 'react-native';

import { getApiBaseUrl, getApiConfigurationError, getExpoBundleUrl } from '@/config/apiBaseUrl';
import { readEnvApiUrl } from '@/config/apiBaseUrlLogic';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

/**
 * Dev-only. Shows Expo bundler URL and LawConnect API URL as separate values.
 */
export function DevApiDiagnostic() {
  const theme = useTheme();
  const isDev = typeof __DEV__ !== 'undefined' && __DEV__;
  const [health, setHealth] = useState('checking…');
  const expoBundleUrl = getExpoBundleUrl() ?? '(unknown)';
  const configError = getApiConfigurationError();
  const apiUrl = configError ? readEnvApiUrl() ?? '(missing EXPO_PUBLIC_API_URL)' : getApiBaseUrl();

  useEffect(() => {
    if (!isDev || configError || !apiUrl.startsWith('http')) {
      setHealth(configError ? 'skipped' : 'checking…');
      return;
    }

    let cancelled = false;
    void (async () => {
      try {
        const response = await fetch(`${apiUrl.replace(/\/$/, '')}/health`);
        const body = (await response.text()).slice(0, 40);
        if (!cancelled) {
          setHealth(response.ok ? `OK ${response.status} ${body}`.trim() : `FAIL ${response.status}`);
        }
      } catch (error) {
        if (!cancelled) {
          setHealth(`FAIL ${error instanceof Error ? error.message : 'network'}`);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [apiUrl, configError, isDev]);

  if (!isDev) {
    return null;
  }

  return (
    <View
      style={{
        backgroundColor: theme.colors.surfaceMuted,
        borderBottomWidth: 1,
        borderBottomColor: theme.colors.border,
        paddingHorizontal: theme.spacing.md,
        paddingVertical: theme.spacing.sm,
        gap: 2,
      }}
    >
      <Text variant="caption" color="secondary">
        Expo bundle URL: {expoBundleUrl}
      </Text>
      <Text variant="caption" color="secondary">
        API URL: {apiUrl}
      </Text>
      <Text variant="caption" color={health.startsWith('OK') ? 'accent' : 'danger'}>
        API /health: {health}
      </Text>
    </View>
  );
}
