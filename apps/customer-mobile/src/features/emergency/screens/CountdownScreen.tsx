import React from 'react';
import { View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { MapBackdrop } from '@/features/emergency/components/MapBackdrop';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useEmergencyStore } from '@/store/emergencyStore';
import { customerSosHeadline } from '@/types/emergency';
import { palette } from '@/theme';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'Countdown'>;

export function CountdownScreen({ navigation }: Props) {
  const theme = useTheme();
  const request = useEmergencyStore((s) => s.request);

  return (
    <MapBackdrop>
      <View
        style={{
          flex: 1,
          paddingTop: 72,
          paddingHorizontal: theme.layout.gutter,
          alignItems: 'center',
        }}
      >
        <Text variant="label" style={{ color: palette.teal400 }}>
          EMERGENCY
        </Text>
        <Text variant="title" style={{ color: palette.white, marginTop: theme.spacing.xs }} align="center">
          {request ? customerSosHeadline(request) : 'Searching for an available lawyer...'}
        </Text>
        <Text variant="body" style={{ color: palette.ink300, marginTop: theme.spacing.xs }} align="center">
          Stay on this screen. We will connect you as soon as a lawyer accepts.
        </Text>

        <View style={{ marginTop: 'auto', marginBottom: theme.spacing['3xl'], width: '100%' }}>
          <Button title="Back to live search" variant="inverse" onPress={() => navigation.goBack()} />
        </View>
      </View>
    </MapBackdrop>
  );
}
