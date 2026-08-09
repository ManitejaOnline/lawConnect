import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { CategorySelectScreen } from '@/features/emergency/screens/CategorySelectScreen';
import { CountdownScreen } from '@/features/emergency/screens/CountdownScreen';
import { EmergencyErrorScreen } from '@/features/emergency/screens/EmergencyErrorScreen';
import { EmergencyHistoryScreen } from '@/features/emergency/screens/EmergencyHistoryScreen';
import { EmergencyHomeScreen } from '@/features/emergency/screens/EmergencyHomeScreen';
import { LawyerAcceptedScreen } from '@/features/emergency/screens/LawyerAcceptedScreen';
import { LawyerFoundScreen } from '@/features/emergency/screens/LawyerFoundScreen';
import { LocationPermissionScreen } from '@/features/emergency/screens/LocationPermissionScreen';
import { RatingScreen } from '@/features/emergency/screens/RatingScreen';
import { SearchingScreen } from '@/features/emergency/screens/SearchingScreen';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

const Stack = createNativeStackNavigator<EmergencyStackParamList>();

export function EmergencyNavigator() {
  const theme = useTheme();

  return (
    <Stack.Navigator
      screenOptions={{
        headerShown: false,
        animation: 'fade_from_bottom',
        contentStyle: { backgroundColor: theme.colors.background },
      }}
    >
      <Stack.Screen name="EmergencyHome" component={EmergencyHomeScreen} />
      <Stack.Screen name="CategorySelect" component={CategorySelectScreen} />
      <Stack.Screen name="LocationConfirm" component={LocationPermissionScreen} />
      <Stack.Screen name="LocationPermission" component={LocationPermissionScreen} />
      <Stack.Screen name="Searching" component={SearchingScreen} options={{ gestureEnabled: false }} />
      <Stack.Screen name="Countdown" component={CountdownScreen} />
      <Stack.Screen name="LawyerFound" component={LawyerFoundScreen} />
      <Stack.Screen name="LawyerAccepted" component={LawyerAcceptedScreen} />
      <Stack.Screen name="Rating" component={RatingScreen} />
      <Stack.Screen name="EmergencyHistory" component={EmergencyHistoryScreen} />
      <Stack.Screen name="EmergencyError" component={EmergencyErrorScreen} />
    </Stack.Navigator>
  );
}
