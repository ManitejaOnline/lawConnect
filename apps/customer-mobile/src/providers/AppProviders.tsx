import React, { useEffect, useMemo, useState } from 'react';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { QueryClientProvider } from '@tanstack/react-query';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import * as SplashScreen from 'expo-splash-screen';
import {
  useFonts,
  PlusJakartaSans_500Medium,
  PlusJakartaSans_600SemiBold,
  PlusJakartaSans_700Bold,
} from '@expo-google-fonts/plus-jakarta-sans';
import { DMSans_400Regular, DMSans_500Medium, DMSans_700Bold } from '@expo-google-fonts/dm-sans';
import * as SystemUI from 'expo-system-ui';
import { StatusBar } from 'expo-status-bar';

import { DevApiDiagnostic } from '@/components/feedback/DevApiDiagnostic';
import { ErrorState } from '@/components/feedback/ErrorState';
import { ToastHost } from '@/components/feedback/ToastHost';
import { API_DEVICE_NOT_CONFIGURED, getApiConfigurationError } from '@/config/apiBaseUrl';
import { RootNavigator } from '@/navigation/RootNavigator';
import { createQueryClient } from '@/services/api/queryClient';
import { ThemeProvider, useTheme } from '@/theme/ThemeProvider';

SplashScreen.preventAutoHideAsync().catch(() => undefined);

function SystemChrome() {
  const theme = useTheme();

  useEffect(() => {
    void SystemUI.setBackgroundColorAsync(theme.colors.background);
  }, [theme.colors.background]);

  return <StatusBar style={theme.scheme === 'dark' ? 'light' : 'dark'} />;
}

export function AppProviders() {
  const [queryClient] = useState(() => createQueryClient());
  const [fontsLoaded] = useFonts({
    PlusJakartaSans_500Medium,
    PlusJakartaSans_600SemiBold,
    PlusJakartaSans_700Bold,
    DMSans_400Regular,
    DMSans_500Medium,
    DMSans_700Bold,
  });

  const ready = fontsLoaded;
  const apiConfigError = getApiConfigurationError();

  useEffect(() => {
    if (ready) {
      void SplashScreen.hideAsync();
    }
  }, [ready]);

  const rootStyle = useMemo(() => ({ flex: 1 }), []);

  if (!ready) {
    return null;
  }

  return (
    <GestureHandlerRootView style={rootStyle}>
      <SafeAreaProvider>
        <QueryClientProvider client={queryClient}>
          <ThemeProvider>
            <SystemChrome />
            <DevApiDiagnostic />
            {apiConfigError ? (
              <ErrorState
                title={API_DEVICE_NOT_CONFIGURED}
                message="Set EXPO_PUBLIC_API_URL to http://<PC-LAN-IP>:5080 and restart Expo. Leave the Expo Go exp:// URL unchanged."
              />
            ) : (
              <>
                <RootNavigator />
                <ToastHost />
              </>
            )}
          </ThemeProvider>
        </QueryClientProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
