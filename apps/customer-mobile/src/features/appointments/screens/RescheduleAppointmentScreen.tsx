import React, { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, View } from 'react-native';
import { useMutation, useQueryClient } from '@tanstack/react-query';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { BottomActionBar } from '@/components/ui/BottomActionBar';
import { Button } from '@/components/ui/Button';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { appointmentKeys } from '@/features/appointments/hooks/useAppointmentQueries';
import { discoveryKeys, useLawyerSlotsQuery } from '@/features/discovery/hooks/useDiscoveryQueries';
import { appointmentsApi } from '@/services/api/lawyersApi';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';
import type { AvailableSlot } from '@/types/home';

function toDateKey(date: Date) {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, '0');
  const d = `${date.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function addDays(base: Date, days: number) {
  const next = new Date(base);
  next.setDate(base.getDate() + days);
  return next;
}

function formatSlotLabel(iso: string) {
  const date = new Date(iso);
  return date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
}

export function RescheduleAppointmentScreen({
  appointmentId,
  lawyerId,
  onBack,
}: {
  appointmentId: string;
  lawyerId: string;
  onBack: () => void;
}) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const queryClient = useQueryClient();
  const days = useMemo(() => Array.from({ length: 14 }, (_, i) => addDays(new Date(), i)), []);
  const [selectedDate, setSelectedDate] = useState(() => toDateKey(new Date()));
  const [selectedSlot, setSelectedSlot] = useState<AvailableSlot | null>(null);
  const slots = useLawyerSlotsQuery(lawyerId, selectedDate);

  const reschedule = useMutation({
    mutationFn: () => {
      if (!selectedSlot) throw new Error('Select a time');
      return appointmentsApi.reschedule(appointmentId, {
        startUtc: selectedSlot.startUtc,
        endUtc: selectedSlot.endUtc,
        timeZoneId: selectedSlot.timeZoneId || 'Asia/Kolkata',
      });
    },
    onSuccess: (appointment) => {
      queryClient.setQueryData(appointmentKeys.detail(appointmentId), appointment);
      void queryClient.invalidateQueries({ queryKey: appointmentKeys.all });
      void queryClient.invalidateQueries({
        queryKey: discoveryKeys.slots(lawyerId, selectedSlot?.startUtc.slice(0, 10) ?? selectedDate),
      });
      showToast({ title: 'Appointment rescheduled', variant: 'success' });
      onBack();
    },
    onError: (error) => {
      showToast({
        title: 'Could not reschedule',
        message: getErrorMessage(error, 'That slot may no longer be available'),
        variant: 'error',
      });
    },
  });

  return (
    <Screen
      edges={['top', 'left', 'right']}
      padded={false}
      footer={
        <View style={{ paddingHorizontal: theme.layout.gutter }}>
          <BottomActionBar>
            <Button
              title="Confirm new time"
              variant="primary"
              size="sm"
              disabled={!selectedSlot || reschedule.isPending}
              loading={reschedule.isPending}
              onPress={() => reschedule.mutate()}
            />
          </BottomActionBar>
        </View>
      }
    >
      <ScrollView
        keyboardShouldPersistTaps="handled"
        contentContainerStyle={{
          paddingHorizontal: theme.layout.gutter,
          paddingBottom: theme.spacing.xl,
          gap: theme.spacing.md,
        }}
      >
        <ScreenHeader title="Reschedule" subtitle="Pick an open slot" onBack={onBack} />

        <Text variant="label" color="secondary">
          DATE
        </Text>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={{ gap: theme.spacing.xs }}>
          {days.map((day) => {
            const key = toDateKey(day);
            const selected = key === selectedDate;
            return (
              <Pressable
                key={key}
                accessibilityRole="button"
                onPress={() => {
                  setSelectedDate(key);
                  setSelectedSlot(null);
                }}
                style={{
                  width: 48,
                  minHeight: 56,
                  borderRadius: theme.radius.md,
                  borderWidth: 1,
                  borderColor: selected ? theme.colors.primary : theme.colors.border,
                  backgroundColor: selected ? theme.colors.primary : theme.colors.surface,
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: 2,
                  paddingVertical: theme.spacing.xs,
                }}
              >
                <Text
                  variant="label"
                  style={{ color: selected ? theme.colors.textInverse : theme.colors.textTertiary }}
                >
                  {day.toLocaleDateString(undefined, { weekday: 'short' })}
                </Text>
                <Text
                  variant="bodyMedium"
                  style={{ color: selected ? theme.colors.textInverse : theme.colors.textPrimary }}
                >
                  {day.getDate()}
                </Text>
              </Pressable>
            );
          })}
        </ScrollView>

        <Text variant="label" color="secondary">
          TIMES
        </Text>
        {slots.isLoading ? (
          <View style={{ gap: theme.spacing.sm }}>
            <Shimmer height={44} radius={theme.radius.md} />
            <Shimmer height={44} radius={theme.radius.md} />
          </View>
        ) : slots.isError ? (
          <ErrorState message="Could not load slots." onRetry={() => slots.refetch()} />
        ) : (slots.data?.length ?? 0) === 0 ? (
          <EmptyState variant="inbox" title="No open slots" description="Try another date." />
        ) : (
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
            {slots.data?.map((slot) => {
              const selected = selectedSlot?.startUtc === slot.startUtc;
              return (
                <Pressable
                  key={`${slot.startUtc}-${slot.endUtc}`}
                  onPress={() => setSelectedSlot(slot)}
                  style={{
                    minWidth: 72,
                    minHeight: 32,
                    paddingHorizontal: theme.spacing.sm,
                    borderRadius: theme.radius.full,
                    borderWidth: 1,
                    borderColor: selected ? theme.colors.primary : theme.colors.border,
                    backgroundColor: selected ? theme.colors.primary : theme.colors.surface,
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                >
                  <Text
                    variant="caption"
                    style={{ color: selected ? theme.colors.textInverse : theme.colors.textPrimary }}
                  >
                    {formatSlotLabel(slot.startUtc)}
                  </Text>
                </Pressable>
              );
            })}
          </View>
        )}
        {slots.isFetching && !slots.isLoading ? <ActivityIndicator color={theme.colors.accent} /> : null}
      </ScrollView>
    </Screen>
  );
}
