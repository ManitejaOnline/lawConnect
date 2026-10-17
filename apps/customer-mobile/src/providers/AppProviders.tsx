import React, { useEffect, useMemo, useState } from 'react';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { QueryClientProvider, useQueryClient } from '@tanstack/react-query';
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
import { stopChatHub } from '@/services/realtime/chatHub';
import { startDocumentHub, stopDocumentHub } from '@/services/realtime/documentHub';
import { stopEmergencyHub } from '@/services/realtime/emergencyHub';
import { useAuthStore } from '@/store/authStore';
import { useChatStore } from '@/store/chatStore';
import { ThemeProvider, useTheme } from '@/theme/ThemeProvider';

SplashScreen.preventAutoHideAsync().catch(() => undefined);

/**
 * Drops everything cached for a signed-in user (queries, chat state, realtime connections)
 * whenever that user signs out or the session is cleared, so the next person on the device
 * never sees or receives the previous user's data.
 */
function SessionScopedDataReset() {
  const queryClient = useQueryClient();

  useEffect(() => {
    let previousUserId = useAuthStore.getState().session?.userId ?? null;
    return useAuthStore.subscribe((state) => {
      const nextUserId = state.session?.userId ?? null;
      if (nextUserId === previousUserId) return;
      const hadUser = previousUserId !== null;
      previousUserId = nextUserId;
      if (!hadUser) return;

      queryClient.clear();
      useChatStore.getState().reset();
      void stopChatHub().catch(() => undefined);
      void stopEmergencyHub().catch(() => undefined);
      void stopDocumentHub().catch(() => undefined);
    });
  }, [queryClient]);

  return null;
}

function DocumentPresence() {
  const userId = useAuthStore((state) => state.session?.userId ?? null);

  useEffect(() => {
    if (!userId) return;
    void startDocumentHub().catch(() => undefined);
    return () => {
      void stopDocumentHub().catch(() => undefined);
    };
  }, [userId]);

  return null;
}

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
          <SessionScopedDataReset />
          <DocumentPresence />
          <ThemeProvider>
            <SystemChrome />
            <DevApiDiagnostic />
            {apiConfigError ? (
              <ErrorState
                title={API_DEVICE_NOT_CONFIGURED}
                message={
                  typeof __DEV__ !== 'undefined' && __DEV__
                    ? 'Set EXPO_PUBLIC_API_URL to your LawConnect API origin and restart Expo. Do not use the Expo Go exp:// URL as the API.'
                    : 'This build is missing a production API address. Reinstall the official LawConnect app.'
                }
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
