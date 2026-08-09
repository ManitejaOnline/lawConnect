import React, { useEffect, useState } from 'react';
import { View } from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { IconButton } from '@/components/ui/IconButton';
import { Screen } from '@/components/layout/Screen';
import { callUserById } from '@/features/calls/openNativeCall';
import { lawyerKeys } from '@/features/lawyer/hooks/useLawyerQueries';
import type { LawyerEmergencyStackParamList } from '@/navigation/types';
import { emergencyApi } from '@/services/api/emergencyApi';
import { joinEmergencyRequest, leaveEmergencyRequest } from '@/services/realtime/emergencyHub';
import { useLawyerEmergencyStore } from '@/store/lawyerEmergencyStore';
import { useToastStore } from '@/store/toastStore';
import { isAssignedStatus } from '@/types/emergency';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<LawyerEmergencyStackParamList, 'LawyerEmergencyActive'>;

export function LawyerEmergencyActiveScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const queryClient = useQueryClient();
  const showToast = useToastStore((s) => s.show);
  const setActive = useLawyerEmergencyStore((s) => s.setActiveRequest);
  const { requestId } = route.params;
  const [calling, setCalling] = useState(false);

  const query = useQuery({
    queryKey: lawyerKeys.sos(requestId),
    queryFn: () => emergencyApi.getSos(requestId),
    refetchInterval: 4_000,
  });

  const request = query.data;

  useEffect(() => {
    void joinEmergencyRequest(requestId);
    return () => {
      void leaveEmergencyRequest(requestId);
    };
  }, [requestId]);

  useEffect(() => {
    if (request) setActive(request);
  }, [request, setActive]);

  const complete = async () => {
    try {
      const updated = await emergencyApi.completeSos(requestId);
      setActive(updated);
      queryClient.setQueryData(lawyerKeys.activeSos(), null);
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.activeSos() });
      void queryClient.invalidateQueries({ queryKey: lawyerKeys.incomingSos() });
      showToast({ title: 'Emergency completed', variant: 'success' });
      navigation.goBack();
    } catch {
      showToast({
        title: 'Could not end emergency',
        message: 'Please try again.',
        variant: 'error',
      });
    }
  };

  return (
    <Screen edges={['top', 'left', 'right']} scroll>
      {query.isLoading && !request ? (
        <LoadingState label="Loading emergency" />
      ) : query.isError || !request ? (
        <ErrorState message="Could not load this emergency." onRetry={() => query.refetch()} />
      ) : (
        <View style={{ gap: theme.spacing.md }}>
          <IconButton name="chevron-left" accessibilityLabel="Go back" onPress={() => navigation.goBack()} />
          <View
            style={{
              marginHorizontal: -theme.layout.gutter,
              backgroundColor: theme.colors.emergency,
              paddingHorizontal: theme.layout.gutter,
              paddingVertical: theme.spacing.md,
            }}
          >
            <Text variant="label" color="inverse">
              SOS ACTIVE
            </Text>
            <Text variant="headline" color="inverse" style={{ marginTop: 4 }}>
              Emergency request
            </Text>
          </View>

          <View style={{ gap: 6 }}>
            <Text variant="caption" color="secondary">
              Case type
            </Text>
            <Text variant="bodyMedium">{request.categoryName}</Text>
            {request.cityName ? (
              <>
                <Text variant="caption" color="secondary">
                  Location
                </Text>
                <Text variant="bodyMedium">{request.cityName}</Text>
              </>
            ) : null}
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
            {request.notes ? (
              <Text variant="body" color="secondary">
                {request.notes}
              </Text>
            ) : null}
          </View>

          {isAssignedStatus(request.status) ? (
            <>
              <Button
                title="Call Customer"
                variant="primary"
                size="sm"
                loading={calling}
                leftSlot={<Icon name="phone" size={16} color={theme.colors.textInverse} />}
                onPress={async () => {
                  setCalling(true);
                  try {
                    await callUserById(request.customerUserId);
                  } finally {
                    setCalling(false);
                  }
                }}
              />
              <Button title="End Emergency" variant="outline" size="sm" onPress={() => void complete()} />
            </>
          ) : (
            <Text variant="body" color="secondary">
              This emergency is no longer active.
            </Text>
          )}
        </View>
      )}
    </Screen>
  );
}
