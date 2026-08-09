import React from 'react';
import { View } from 'react-native';
import { useMutation } from '@tanstack/react-query';

import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import {
  useAppointmentQuery,
  useCancelAppointmentMutation,
} from '@/features/appointments/hooks/useAppointmentQueries';
import { callUserById } from '@/features/calls/openNativeCall';
import { chatApi } from '@/services/api/chatApi';
import { useToastStore } from '@/store/toastStore';
import {
  appointmentPartyName,
  formatAppointmentWhen,
} from '@/types/appointment';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';

export function AppointmentDetailScreen({
  appointmentId,
  perspective,
  onBack,
  onOpenChat,
}: {
  appointmentId: string;
  perspective: 'lawyer' | 'customer';
  onBack?: () => void;
  onOpenChat: (args: {
    conversationId: string;
    title: string;
    otherUserId: string;
  }) => void;
}) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const query = useAppointmentQuery(appointmentId);
  const cancel = useCancelAppointmentMutation();
  const item = query.data;
  const scheduled = String(item?.status ?? '').toLowerCase() === 'scheduled';
  const otherUserId = perspective === 'lawyer' ? item?.customerUserId : item?.lawyerUserId;
  const otherName = item ? appointmentPartyName(item, perspective) : '';

  const openChat = useMutation({
    mutationFn: async () => {
      if (!otherUserId) throw new Error('Missing participant');
      return chatApi.openConversation(otherUserId);
    },
    onSuccess: (conv) => {
      onOpenChat({
        conversationId: conv.conversationId,
        title: otherName,
        otherUserId: conv.otherUserId,
      });
    },
    onError: (error) => {
      showToast({
        title: 'Chat failed',
        message: getErrorMessage(error, 'Could not open conversation'),
        variant: 'error',
      });
    },
  });

  const callLabel = perspective === 'lawyer' ? 'Call Customer' : 'Call Lawyer';

  return (
    <Screen edges={['top', 'left', 'right']} scroll>
      <ScreenHeader title="Appointment" subtitle="Booking details" onBack={onBack} />

      {query.isLoading && !item ? (
        <LoadingState label="Loading appointment" />
      ) : query.isError || !item ? (
        <ErrorState message="Could not load this appointment." onRetry={() => query.refetch()} />
      ) : (
        <View style={{ gap: theme.spacing.md }}>
          <Card>
            <View style={{ gap: theme.spacing.xs }}>
              <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
                <Text variant="headline">{otherName}</Text>
                <Badge label={item.status} />
              </View>
              <Text variant="body" color="secondary">
                {formatAppointmentWhen(item.startUtc, item.timeZoneId)}
              </Text>
              <Text variant="caption" color="secondary">
                Ends {formatAppointmentWhen(item.endUtc, item.timeZoneId)}
              </Text>
              <Text variant="body">Fee ₹{item.consultationFeeSnapshot}</Text>
              {item.notes ? (
                <Text variant="caption" color="secondary">
                  Notes: {item.notes}
                </Text>
              ) : null}
              {item.cancellationReason ? (
                <Text variant="caption" color="danger">
                  Cancelled: {item.cancellationReason}
                </Text>
              ) : null}
            </View>
          </Card>

          <Button
            title="Message"
            variant="accent"
            loading={openChat.isPending}
            onPress={() => openChat.mutate()}
          />
          {scheduled ? (
            <>
              <Button
                title={callLabel}
                variant="secondary"
                leftSlot={<Icon name="phone" size={18} color={theme.colors.textPrimary} />}
                onPress={() => void callUserById(otherUserId)}
              />
              <Button
                title="Cancel appointment"
                variant="danger"
                loading={cancel.isPending}
                onPress={() =>
                  cancel.mutate(
                    { appointmentId: item.id },
                    {
                      onSuccess: () => showToast({ title: 'Cancelled', variant: 'info' }),
                      onError: (error) =>
                        showToast({
                          title: 'Cancel failed',
                          message: getErrorMessage(error, 'Could not cancel'),
                          variant: 'error',
                        }),
                    },
                  )
                }
              />
            </>
          ) : null}
        </View>
      )}
    </Screen>
  );
}
