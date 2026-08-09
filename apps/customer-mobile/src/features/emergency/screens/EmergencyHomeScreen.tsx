import React, { useEffect } from 'react';
import { Pressable, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { useRestoreActiveEmergency } from '@/features/emergency/hooks/useRestoreActiveEmergency';
import { useDeviceLocation } from '@/hooks/useDeviceLocation';
import { useLocationLabel } from '@/hooks/useLocationLabel';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useEmergencyStore } from '@/store/emergencyStore';
import { isAssignedStatus, isSearchingStatus, resolveEmergencyStatus } from '@/types/emergency';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'EmergencyHome'>;

export function EmergencyHomeScreen({ navigation }: Props) {
  const theme = useTheme();
  const resetSession = useEmergencyStore((s) => s.resetSession);
  const restoreActiveSos = useRestoreActiveEmergency();
  const { location } = useDeviceLocation();
  const locationLabel = useLocationLabel(location);

  useEffect(() => {
    let cancelled = false;
    void restoreActiveSos().then((active) => {
      if (cancelled || !active) return;
      const status = resolveEmergencyStatus(active);
      if (isAssignedStatus(status)) {
        navigation.replace('LawyerFound');
        return;
      }
      if (isSearchingStatus(status)) {
        navigation.replace('Searching', {
          categoryId: active.categoryId,
          categoryTitle: active.categoryName,
          latitude: active.latitude,
          longitude: active.longitude,
          notes: active.notes ?? undefined,
          requestId: active.requestId,
        });
      }
    });
    return () => {
      cancelled = true;
    };
  }, [navigation, restoreActiveSos]);

  const startSos = () => {
    resetSession();
    navigation.navigate('CategorySelect');
  };

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']}>
      <View style={{ flex: 1 }}>
        <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
          <Pressable accessibilityRole="button" onPress={() => navigation.getParent()?.goBack()} hitSlop={theme.hitSlop.md}>
            <Icon name="chevron-left" size={22} color={theme.colors.textPrimary} />
          </Pressable>
          <Text variant="bodyMedium">Emergency SOS</Text>
          <Pressable accessibilityRole="button" onPress={() => navigation.navigate('EmergencyHistory')} hitSlop={theme.hitSlop.md}>
            <Icon name="clock" size={18} color={theme.colors.textSecondary} />
          </Pressable>
        </View>

        <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', gap: theme.spacing.md }}>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Tap to send SOS"
            onPress={startSos}
            style={{
              width: 140,
              height: 140,
              borderRadius: 70,
              backgroundColor: theme.colors.emergency,
              alignItems: 'center',
              justifyContent: 'center',
              ...theme.elevation.md,
            }}
          >
            <Text variant="title" color="inverse" style={{ letterSpacing: 1 }}>
              SOS
            </Text>
          </Pressable>
          <Text variant="bodyMedium">Tap to Send SOS</Text>
          <Text variant="caption" color="secondary" align="center">
            Nearby emergency-ready lawyers are notified. Only a real assignment connects you.
          </Text>
        </View>

        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, paddingBottom: theme.spacing.lg }}>
          <Icon name="map-pin" size={14} color={theme.colors.primary} />
          <Text variant="caption" color="secondary" numberOfLines={2} style={{ flex: 1 }}>
            {locationLabel}
          </Text>
        </View>
      </View>
    </Screen>
  );
}
