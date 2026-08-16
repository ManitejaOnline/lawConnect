import React from 'react';
import { NavigationContainer, DarkTheme, DefaultTheme } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { useAuthBootstrap } from '@/features/auth/hooks/useAuthBootstrap';
import { useResolvedAppSurface } from '@/features/auth/hooks/useResolvedAppSurface';
import { AccessBlockedScreen } from '@/features/auth/screens/AccessBlockedScreen';
import { SplashScreen } from '@/features/auth/screens/SplashScreen';
import { LawyerOnboardingScreen } from '@/features/lawyer/screens/LawyerOnboardingScreen';
import { LawyerPendingScreen } from '@/features/lawyer/screens/LawyerPendingScreen';
import { AdminTabNavigator } from '@/navigation/AdminTabNavigator';
import { AuthNavigator } from '@/navigation/AuthNavigator';
import { EmergencyNavigator } from '@/navigation/EmergencyNavigator';
import { LawyerTabNavigator } from '@/navigation/LawyerTabNavigator';
import { MainTabNavigator } from '@/navigation/MainTabNavigator';
import type { RootStackParamList } from '@/navigation/types';
import { useAuthStore } from '@/store/authStore';
import { useTheme } from '@/theme/ThemeProvider';
import { useEmergencyDeepLink } from '@/features/emergency/hooks/useEmergencyDeepLink';
import { navigationRef } from '@/navigation/navigationRef';

const Stack = createNativeStackNavigator<RootStackParamList>();

export function RootNavigator() {
  const theme = useTheme();
  const authenticated = useAuthStore((s) => Boolean(s.session?.accessToken));
  const { ready, statusLabel } = useAuthBootstrap();
  const { surface, ready: surfaceReady } = useResolvedAppSurface();
  useEmergencyDeepLink();

  const navTheme =
    theme.scheme === 'dark'
      ? {
          ...DarkTheme,
          colors: {
            ...DarkTheme.colors,
            background: theme.colors.background,
            card: theme.colors.surface,
            text: theme.colors.textPrimary,
            border: theme.colors.border,
            primary: theme.colors.accent,
          },
        }
      : {
          ...DefaultTheme,
          colors: {
            ...DefaultTheme.colors,
            background: theme.colors.background,
            card: theme.colors.surface,
            text: theme.colors.textPrimary,
            border: theme.colors.border,
            primary: theme.colors.accent,
          },
        };

  if (!ready || (authenticated && !surfaceReady)) {
    return <SplashScreen statusLabel={statusLabel} />;
  }

  const showCustomer = authenticated && surface === 'customer';
  const showLawyerHome = authenticated && surface === 'lawyer-home';
  const showLawyerOnboarding = authenticated && surface === 'lawyer-onboarding';
  const showLawyerPending = authenticated && surface === 'lawyer-pending';
  const showAdmin = authenticated && surface === 'admin';
  const showBlocked = authenticated && surface === 'wrong-app';

  return (
    <NavigationContainer theme={navTheme} ref={navigationRef}>
      <Stack.Navigator key={surface} screenOptions={{ headerShown: false, animation: 'fade' }}>
        {showCustomer ? (
          <>
            <Stack.Screen name="Main" component={MainTabNavigator} />
            <Stack.Screen
              name="Emergency"
              component={EmergencyNavigator}
              options={{ presentation: 'fullScreenModal', animation: 'slide_from_bottom' }}
            />
          </>
        ) : showLawyerHome ? (
          <Stack.Screen name="LawyerMain" component={LawyerTabNavigator} />
        ) : showLawyerOnboarding ? (
          <Stack.Screen name="LawyerOnboarding" component={LawyerOnboardingScreen} />
        ) : showLawyerPending ? (
          <Stack.Screen name="LawyerPending" component={LawyerPendingScreen} />
        ) : showAdmin ? (
          <Stack.Screen name="AdminMain" component={AdminTabNavigator} />
        ) : showBlocked ? (
          <Stack.Screen name="AccessBlocked">{() => <AccessBlockedScreen surface="wrong-app" />}</Stack.Screen>
        ) : (
          <Stack.Screen name="Auth" component={AuthNavigator} />
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}
