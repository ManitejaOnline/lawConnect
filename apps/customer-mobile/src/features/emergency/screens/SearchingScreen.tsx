import React, { useEffect, useRef, useState } from 'react';
import { View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { MapBackdrop } from '@/features/emergency/components/MapBackdrop';
import { RippleSearch } from '@/features/emergency/components/RippleSearch';
import { useEmergencySession } from '@/features/emergency/hooks/useEmergencySession';
import { emergencyApi } from '@/services/api/emergencyApi';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useEmergencyStore } from '@/store/emergencyStore';
import { customerSosHeadline } from '@/types/emergency';
import { palette } from '@/theme';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'Searching'>;

function formatElapsed(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

export function SearchingScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const { startSos, cancelSos, applyRequest } = useEmergencySession();
  const started = useRef(false);
  const startedAt = useRef(Date.now());
  const phase = useEmergencyStore((s) => s.phase);
  const request = useEmergencyStore((s) => s.request);
  const [elapsed, setElapsed] = useState(0);

  useEffect(() => {
    if (started.current) return;
    started.current = true;
    startedAt.current = Date.now();
    if (route.params.requestId) {
      void emergencyApi
        .getSos(route.params.requestId)
        .then((fresh) => applyRequest(fresh))
        .catch(() => undefined);
      return;
    }
    void startSos({
      categoryId: route.params.categoryId,
      latitude: route.params.latitude,
      longitude: route.params.longitude,
      notes: route.params.notes,
    }).catch(() => undefined);
  }, [applyRequest, route.params, startSos]);

  useEffect(() => {
    const timer = setInterval(() => {
      setElapsed(Math.floor((Date.now() - startedAt.current) / 1000));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    if (phase === 'accepted') {
      navigation.replace('LawyerFound');
    }
    if (phase === 'error') {
      navigation.replace('EmergencyError', {
        kind: useEmergencyStore.getState().errorKind ?? 'generic',
        message: useEmergencyStore.getState().errorMessage ?? 'Something went wrong.',
      });
    }
  }, [navigation, phase]);

  return (
    <MapBackdrop>
      <View style={{ flex: 1, paddingTop: 56, paddingHorizontal: theme.layout.gutter }}>
        <Text variant="label" style={{ color: palette.teal400 }}>
          {route.params.categoryTitle.toUpperCase()}
        </Text>
        <Text variant="title" style={{ color: palette.white, marginTop: theme.spacing.xs }}>
          {request ? customerSosHeadline(request) : 'Searching for an available lawyer...'}
        </Text>
        <Text variant="body" style={{ color: palette.ink300, marginTop: theme.spacing.xs }}>
          Stay on this screen. We will connect you as soon as a lawyer accepts.
        </Text>

        <View style={{ alignItems: 'center', marginTop: theme.spacing.xl }}>
          <RippleSearch />
          <Text
            variant="headline"
            style={{ color: palette.white, marginTop: theme.spacing.lg }}
            accessibilityLabel={`Elapsed time ${formatElapsed(elapsed)}`}
          >
            {formatElapsed(elapsed)}
          </Text>
          <Text variant="caption" style={{ color: palette.ink300 }}>
            Elapsed time
          </Text>
        </View>

        <View style={{ marginTop: 'auto', marginBottom: theme.spacing['3xl'] }}>
          <Button
            title="Cancel Request"
            variant="inverse"
            onPress={async () => {
              try {
                await cancelSos('Cancelled by customer');
              } finally {
                navigation.popToTop();
              }
            }}
          />
        </View>
      </View>
    </MapBackdrop>
  );
}
