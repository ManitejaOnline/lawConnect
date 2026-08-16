import React, { useState } from 'react';
import { View } from 'react-native';
import { useMutation, useQueryClient } from '@tanstack/react-query';

import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { AppModal } from '@/components/ui/Modal';
import { Rating } from '@/components/ui/Rating';
import { Text } from '@/components/ui/Text';
import { TextField } from '@/components/ui/TextField';
import { Icon } from '@/components/icons/Icon';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import {
  appointmentKeys,
  useAppointmentQuery,
  useCancelAppointmentMutation,
} from '@/features/appointments/hooks/useAppointmentQueries';
import { callUserById } from '@/features/calls/openNativeCall';
import { chatApi } from '@/services/api/chatApi';
import { appointmentsApi } from '@/services/api/lawyersApi';
import { ratingsApi } from '@/services/api/ratingsApi';
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
  onCreateCase,
}: {
  appointmentId: string;
  perspective: 'lawyer' | 'customer';
  onBack?: () => void;
  onOpenChat: (args: {
    conversationId: string;
    title: string;
    otherUserId: string;
  }) => void;
  onCreateCase?: (args: { customerId: string; customerName: string; sourceAppointmentId: string }) => void;
}) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const queryClient = useQueryClient();
  const query = useAppointmentQuery(appointmentId);
  const cancel = useCancelAppointmentMutation();
  const item = query.data;
  const status = String(item?.status ?? '').toLowerCase();
  const scheduled = status === 'scheduled';
  const completed = status === 'completed';
  const otherUserId = perspective === 'lawyer' ? item?.customerUserId : item?.lawyerUserId;
  const otherName = item ? appointmentPartyName(item, perspective) : '';
  const canReport =
    perspective === 'customer' && scheduled && item ? new Date(item.endUtc).getTime() < Date.now() : false;

  const [showRatingModal, setShowRatingModal] = useState(false);
  const [stars, setStars] = useState(5);
  const [reviewText, setReviewText] = useState('');
  const [showComplaintModal, setShowComplaintModal] = useState(false);
  const [complaintCategory, setComplaintCategory] = useState('');
  const [complaintDescription, setComplaintDescription] = useState('');

  const submitRating = useMutation({
    mutationFn: () => {
      if (!item) throw new Error('Missing appointment');
      return ratingsApi.submit(item.lawyerId, item.id, stars, reviewText.trim() || undefined);
    },
    onSuccess: () => {
      setShowRatingModal(false);
      setReviewText('');
      showToast({ title: 'Thanks for your rating', variant: 'success' });
      void queryClient.invalidateQueries({ queryKey: appointmentKeys.detail(appointmentId) });
    },
    onError: (error) => {
      showToast({
        title: 'Could not submit rating',
        message: getErrorMessage(error, 'You may have already rated this appointment'),
        variant: 'error',
      });
    },
  });

  const submitComplaint = useMutation({
    mutationFn: () => {
      if (!item) throw new Error('Missing appointment');
      return ratingsApi.submitComplaint(item.lawyerId, complaintCategory.trim(), complaintDescription.trim(), item.id);
    },
    onSuccess: () => {
      setShowComplaintModal(false);
      setComplaintCategory('');
      setComplaintDescription('');
      showToast({ title: 'Report submitted', message: 'Our admin team will review it.', variant: 'success' });
    },
    onError: (error) => {
      showToast({
        title: 'Could not submit report',
        message: getErrorMessage(error, 'Please try again'),
        variant: 'error',
      });
    },
  });

  const reportNoShow = useMutation({
    mutationFn: () => appointmentsApi.reportNoShow(appointmentId),
    onSuccess: () => {
      showToast({ title: 'Reported', message: 'Thanks — we recorded this appointment as a no-show.', variant: 'info' });
      void queryClient.invalidateQueries({ queryKey: appointmentKeys.all });
    },
    onError: (error) => {
      showToast({
        title: 'Could not report no-show',
        message: getErrorMessage(error, 'Please try again'),
        variant: 'error',
      });
    },
  });

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
    <>
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
          {perspective === 'lawyer' && onCreateCase ? (
            <Button
              title="Create Case"
              variant="secondary"
              leftSlot={<Icon name="briefcase" size={16} color={theme.colors.textPrimary} />}
              onPress={() =>
                onCreateCase({ customerId: item.customerId, customerName: otherName, sourceAppointmentId: item.id })
              }
            />
          ) : null}
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
              {canReport ? (
                <Button
                  title="Report lawyer no-show"
                  variant="outline"
                  loading={reportNoShow.isPending}
                  onPress={() => reportNoShow.mutate()}
                />
              ) : null}
            </>
          ) : null}
          {perspective === 'customer' && completed ? (
            <>
              {item.hasRating !== true ? (
                <Button title="Rate this lawyer" variant="primary" onPress={() => setShowRatingModal(true)} />
              ) : (
                <Text variant="caption" color="secondary" style={{ textAlign: 'center' }}>
                  You've already rated this appointment
                </Text>
              )}
              <Button title="Report an issue" variant="outline" onPress={() => setShowComplaintModal(true)} />
            </>
          ) : null}
        </View>
      )}
    </Screen>

    <AppModal
      visible={showRatingModal}
      onClose={() => setShowRatingModal(false)}
      title="Rate this lawyer"
      footer={
        <>
          <Button title="Submit rating" variant="primary" loading={submitRating.isPending} onPress={() => submitRating.mutate()} />
          <Button title="Cancel" variant="ghost" onPress={() => setShowRatingModal(false)} />
        </>
      }
    >
      <View style={{ alignItems: 'center', gap: theme.spacing.md }}>
        <Rating value={stars} interactive size={32} onChange={setStars} />
        <TextField
          label="Review (optional)"
          value={reviewText}
          onChangeText={setReviewText}
          multiline
          placeholder="How was your consultation?"
          containerStyle={{ width: '100%' }}
        />
      </View>
    </AppModal>

    <AppModal
      visible={showComplaintModal}
      onClose={() => setShowComplaintModal(false)}
      title="Report an issue"
      footer={
        <>
          <Button
            title="Submit report"
            variant="danger"
            disabled={!complaintCategory.trim() || !complaintDescription.trim()}
            loading={submitComplaint.isPending}
            onPress={() => submitComplaint.mutate()}
          />
          <Button title="Cancel" variant="ghost" onPress={() => setShowComplaintModal(false)} />
        </>
      }
    >
      <View style={{ gap: theme.spacing.md }}>
        <Text variant="body" color="secondary">
          This is sent to LawConnect admins for review — it will not affect this lawyer's score unless verified.
        </Text>
        <TextField label="Category" value={complaintCategory} onChangeText={setComplaintCategory} placeholder="e.g. Missed appointment" />
        <TextField
          label="Description"
          value={complaintDescription}
          onChangeText={setComplaintDescription}
          multiline
          placeholder="What happened?"
        />
      </View>
    </AppModal>
    </>
  );
}
