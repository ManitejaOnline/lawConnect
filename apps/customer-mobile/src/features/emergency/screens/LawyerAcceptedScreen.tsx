import React, { useEffect, useState } from 'react';
import { View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { EmergencyLawyerCard } from '@/features/emergency/components/EmergencyLawyerCard';
import { callUserById } from '@/features/calls/openNativeCall';
import { useEmergencySession } from '@/features/emergency/hooks/useEmergencySession';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useEmergencyStore } from '@/store/emergencyStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'LawyerAccepted'>;

export function LawyerAcceptedScreen({ navigation }: Props) {
  const theme = useTheme();
  const { cancelSos } = useEmergencySession();
  const lawyer = useEmergencyStore((s) => s.assignedLawyer);
  const request = useEmergencyStore((s) => s.request);
  const phase = useEmergencyStore((s) => s.phase);
  const [calling, setCalling] = useState(false);

  useEffect(() => {
    if (phase === 'error') {
      navigation.replace('EmergencyError', {
        kind: useEmergencyStore.getState().errorKind ?? 'lawyer_cancelled',
        message: useEmergencyStore.getState().errorMessage ?? 'Lawyer cancelled the SOS.',
      });
    }
  }, [navigation, phase]);

  if (!lawyer) {
    return (
      <Screen edges={['top', 'left', 'right', 'bottom']}>
        <ScreenHeader title="Lawyer Assigned" />
        <Text variant="body" color="secondary">
          Loading lawyer details…
        </Text>
      </Screen>
    );
  }

  return (
    <Screen
      edges={['top', 'left', 'right', 'bottom']}
      footer={
        <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.sm }}>
          <Button
            title="Call Lawyer"
            variant="accent"
            loading={calling}
            leftSlot={<Icon name="phone" size={18} color={theme.colors.textInverse} />}
            onPress={async () => {
              setCalling(true);
              try {
                await callUserById(lawyer.userId);
              } finally {
                setCalling(false);
              }
            }}
          />
          <Button
            title="Cancel"
            variant="ghost"
            onPress={async () => {
              await cancelSos('Cancelled after accept');
              navigation.popToTop();
            }}
          />
        </View>
      }
    >
      <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.md, flex: 1 }}>
        <ScreenHeader title="Lawyer Assigned" subtitle="Help is on the way" />
        <Text variant="body" color="secondary">
          {request?.categoryName ?? 'Emergency'}
        </Text>
        <EmergencyLawyerCard lawyer={lawyer} />
      </View>
    </Screen>
  );
}
