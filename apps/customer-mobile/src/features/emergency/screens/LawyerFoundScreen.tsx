import React, { useEffect, useState } from 'react';
import { View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { EmergencyLawyerCard } from '@/features/emergency/components/EmergencyLawyerCard';
import { callUserById } from '@/features/calls/openNativeCall';
import { useEmergencySession } from '@/features/emergency/hooks/useEmergencySession';
import type { EmergencyStackParamList } from '@/navigation/types';
import { lawyersApi } from '@/services/api/lawyersApi';
import { useEmergencyStore } from '@/store/emergencyStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'LawyerFound'>;

export function LawyerFoundScreen({ navigation }: Props) {
  const theme = useTheme();
  const { cancelSos } = useEmergencySession();
  const lawyer = useEmergencyStore((s) => s.assignedLawyer);
  const request = useEmergencyStore((s) => s.request);
  const phase = useEmergencyStore((s) => s.phase);
  const setAssignedLawyer = useEmergencyStore((s) => s.setAssignedLawyer);
  const [calling, setCalling] = useState(false);

  useEffect(() => {
    if (phase === 'error') {
      navigation.replace('EmergencyError', {
        kind: useEmergencyStore.getState().errorKind ?? 'lawyer_cancelled',
        message: useEmergencyStore.getState().errorMessage ?? 'Assignment was lost.',
      });
    }
  }, [navigation, phase]);

  useEffect(() => {
    const lawyerId = request?.assignedLawyerId;
    if (!lawyerId || lawyer) return;
    let cancelled = false;
    void lawyersApi
      .getById(lawyerId)
      .then((profile) => {
        if (!cancelled) setAssignedLawyer(profile);
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, [lawyer, request?.assignedLawyerId, setAssignedLawyer]);

  return (
    <Screen
      edges={['top', 'left', 'right', 'bottom']}
      footer={
        <View style={{ gap: theme.spacing.sm }}>
          <Button
            title="Call Lawyer"
            variant="primary"
            size="sm"
            disabled={!lawyer}
            loading={calling}
            leftSlot={<Icon name="phone" size={16} color={theme.colors.textInverse} />}
            onPress={async () => {
              if (!lawyer) return;
              setCalling(true);
              try {
                await callUserById(lawyer.userId);
              } finally {
                setCalling(false);
              }
            }}
          />
          <Button
            title="Cancel SOS"
            variant="danger"
            size="sm"
            onPress={async () => {
              await cancelSos('Cancelled after accept');
              navigation.popToTop();
            }}
          />
        </View>
      }
    >
      <View
        style={{
          marginHorizontal: -theme.layout.gutter,
          backgroundColor: theme.colors.emergency,
          paddingHorizontal: theme.layout.gutter,
          paddingVertical: theme.spacing.md,
          marginBottom: theme.spacing.md,
        }}
      >
        <Text variant="label" color="inverse">
          SOS ACTIVE
        </Text>
        <Text variant="headline" color="inverse" style={{ marginTop: 4 }}>
          Help is on the way!
        </Text>
      </View>

      <View style={{ gap: theme.spacing.sm }}>
        <Text variant="caption" color="secondary">
          Case type
        </Text>
        <Text variant="bodyMedium">{request?.categoryName ?? 'Emergency'}</Text>
        {request?.cityName ? (
          <>
            <Text variant="caption" color="secondary">
              Location
            </Text>
            <Text variant="bodyMedium">{request.cityName}</Text>
          </>
        ) : null}
        {request?.createdAtUtc ? (
          <>
            <Text variant="caption" color="secondary">
              Time
            </Text>
            <Text variant="bodyMedium">
              {new Date(request.createdAtUtc).toLocaleString(undefined, {
                hour: 'numeric',
                minute: '2-digit',
                day: 'numeric',
                month: 'short',
              })}
            </Text>
          </>
        ) : null}

        {lawyer ? (
          <EmergencyLawyerCard lawyer={lawyer} />
        ) : (
          <Text variant="body" color="secondary">
            Loading assigned lawyer details…
          </Text>
        )}
      </View>
    </Screen>
  );
}
