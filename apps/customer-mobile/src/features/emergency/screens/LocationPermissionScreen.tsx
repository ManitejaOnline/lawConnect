import React, { useState } from 'react';
import { View } from 'react-native';
import * as Location from 'expo-location';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useEmergencyStore } from '@/store/emergencyStore';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'LocationConfirm' | 'LocationPermission'>;

type Coords = { latitude: number; longitude: number };

export function LocationPermissionScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const storeCategoryId = useEmergencyStore((s) => s.selectedCategoryId);
  const storeCategoryTitle = useEmergencyStore((s) => s.selectedCategoryTitle);
  const [loading, setLoading] = useState(false);
  const [coords, setCoords] = useState<Coords | null>(null);

  const categoryId = route.params?.categoryId ?? storeCategoryId;
  const categoryTitle = route.params?.categoryTitle ?? storeCategoryTitle ?? 'Emergency';
  const notes = route.params?.notes;

  const readLocation = async () => {
    if (!categoryId) {
      navigation.replace('CategorySelect');
      return;
    }

    setLoading(true);
    try {
      const permission = await Location.requestForegroundPermissionsAsync();
      if (permission.status !== 'granted') {
        navigation.replace('EmergencyError', {
          kind: 'location',
          message: 'Location is required to find nearby emergency lawyers.',
        });
        return;
      }

      const position = await Location.getCurrentPositionAsync({
        accuracy: Location.Accuracy.Balanced,
      });
      setCoords({
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
      });
    } catch {
      showToast({
        title: 'Location unavailable',
        message: 'We could not read your position.',
        variant: 'error',
      });
      navigation.replace('EmergencyError', {
        kind: 'location',
        message: 'Location services are unavailable right now.',
      });
    } finally {
      setLoading(false);
    }
  };

  const confirm = () => {
    if (!categoryId || !coords) return;
    navigation.replace('Searching', {
      categoryId,
      categoryTitle,
      latitude: coords.latitude,
      longitude: coords.longitude,
      notes,
    });
  };

  return (
    <Screen
      edges={['top', 'left', 'right', 'bottom']}
      footer={
        <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.sm }}>
          {coords ? (
            <Button
              title="Confirm Location"
              variant="accent"
              onPress={confirm}
              accessibilityHint="Shares this location with the responding lawyer"
            />
          ) : (
            <Button
              title="Use current location"
              variant="accent"
              loading={loading}
              onPress={() => void readLocation()}
              accessibilityHint="Reads GPS so you can confirm before searching"
            />
          )}
          <Button title="Not now" variant="ghost" onPress={() => navigation.goBack()} />
        </View>
      }
    >
      <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.lg, flex: 1 }}>
        <ScreenHeader title="Confirm location" subtitle={categoryTitle} onBack={() => navigation.goBack()} />
        <View
          style={{
            width: 72,
            height: 72,
            borderRadius: theme.radius.full,
            backgroundColor: theme.colors.accentSoft,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Icon name="shield" size={32} color={theme.colors.accent} />
        </View>
        <Text variant="title">Share your location</Text>
        <Text variant="body" color="secondary">
          Share your current location with the responding lawyer. Matching only includes
          emergency-available lawyers near this point — a lawyer in another city will not receive
          the request.
        </Text>
        {coords ? (
          <View style={{ gap: theme.spacing.xxs }}>
            <Text variant="label" color="secondary">
              LOCATION TO SHARE
            </Text>
            <Text variant="bodyMedium">
              {coords.latitude.toFixed(4)}, {coords.longitude.toFixed(4)}
            </Text>
            <Button title="Refresh location" variant="ghost" size="sm" onPress={() => void readLocation()} />
          </View>
        ) : null}
      </View>
    </Screen>
  );
}
