import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { CustomerSignupScreen } from '@/features/auth/screens/CustomerSignupScreen';
import { LawyerSignupScreen } from '@/features/auth/screens/LawyerSignupScreen';
import { LoginScreen } from '@/features/auth/screens/LoginScreen';
import { OnboardingScreen } from '@/features/auth/screens/OnboardingScreen';
import { RoleSelectScreen } from '@/features/auth/screens/RoleSelectScreen';
import type { AuthStackParamList } from '@/navigation/types';
import { useAuthStore } from '@/store/authStore';
import { useTheme } from '@/theme/ThemeProvider';

const Stack = createNativeStackNavigator<AuthStackParamList>();

export function AuthNavigator() {
  const theme = useTheme();
  const onboardingCompleted = useAuthStore((s) => s.onboardingCompleted);

  return (
    <Stack.Navigator
      initialRouteName={onboardingCompleted ? 'RoleSelect' : 'Onboarding'}
      screenOptions={{
        headerShown: false,
        contentStyle: { backgroundColor: theme.colors.background },
        animation: 'fade_from_bottom',
        gestureEnabled: true,
      }}
    >
      <Stack.Screen name="Onboarding" component={OnboardingScreen} />
      <Stack.Screen name="RoleSelect" component={RoleSelectScreen} />
      <Stack.Screen name="Login" component={LoginScreen} />
      <Stack.Screen name="CustomerSignup" component={CustomerSignupScreen} />
      <Stack.Screen name="LawyerSignup" component={LawyerSignupScreen} />
    </Stack.Navigator>
  );
}
