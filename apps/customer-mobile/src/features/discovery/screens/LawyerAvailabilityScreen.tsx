import React, { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { ErrorState } from '@/components/feedback/ErrorState';
import { EmptyState } from '@/components/feedback/EmptyState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { BottomActionBar } from '@/components/ui/BottomActionBar';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Text } from '@/components/ui/Text';
import { Avatar } from '@/components/ui/Avatar';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import {
  useBookAppointmentMutation,
  useLawyerProfileQuery,
  useLawyerSlotsQuery,
} from '@/features/discovery/hooks/useDiscoveryQueries';
import type { SearchStackParamList } from '@/navigation/types';
import { useToastStore } from '@/store/toastStore';
import { useTheme } from '@/theme/ThemeProvider';
import { getErrorMessage } from '@/utils/errors';
import type { AvailableSlot } from '@/types/home';

type Props = NativeStackScreenProps<SearchStackParamList, 'LawyerAvailability'>;

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

export function LawyerAvailabilityScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const showToast = useToastStore((s) => s.show);
  const { lawyerId } = route.params;
  const profile = useLawyerProfileQuery(lawyerId);

  const days = useMemo(() => Array.from({ length: 14 }, (_, i) => addDays(new Date(), i)), []);
  const [selectedDate, setSelectedDate] = useState(() => toDateKey(new Date()));
  const [selectedSlot, setSelectedSlot] = useState<AvailableSlot | null>(null);

  const slots = useLawyerSlotsQuery(lawyerId, selectedDate);
  const book = useBookAppointmentMutation();

  const onBook = () => {
    if (!selectedSlot) return;
    book.mutate(
      {
        lawyerId,
        startUtc: selectedSlot.startUtc,
        endUtc: selectedSlot.endUtc,
        timeZoneId: selectedSlot.timeZoneId || 'Asia/Kolkata',
      },
      {
        onSuccess: () => {
          showToast({
            title: 'Appointment booked',
            message: 'You can manage it from Bookings.',
            variant: 'success',
          });
          navigation.navigate('LawyerProfile', { lawyerId });
        },
        onError: (error) => {
          showToast({
            title: 'Booking failed',
            message: getErrorMessage(error, 'Could not book this slot'),
            variant: 'error',
          });
        },
      },
    );
  };

  return (
    <Screen
      edges={['top', 'left', 'right']}
      padded={false}
      footer={
        <View style={{ paddingHorizontal: theme.layout.gutter }}>
          <BottomActionBar>
            <Button
              title="Continue"
              variant="primary"
              disabled={!selectedSlot || book.isPending}
              loading={book.isPending}
              onPress={onBook}
              rightSlot={
                profile.data ? (
                  <Text variant="bodyMedium" color="inverse">
                    ₹{Math.round(profile.data.consultationFee).toLocaleString('en-IN')}
                  </Text>
                ) : null
              }
            />
          </BottomActionBar>
        </View>
      }
    >
      <ScrollView
        contentContainerStyle={{
          paddingHorizontal: theme.layout.gutter,
          paddingBottom: theme.spacing.xl,
          gap: theme.spacing.md,
        }}
        showsVerticalScrollIndicator={false}
      >
        <ScreenHeader title="Book appointment" onBack={() => navigation.goBack()} />

        {profile.data ? (
          <Card>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm }}>
              <Avatar
                uri={profile.data.profileImageUrl}
                name={`${profile.data.firstName} ${profile.data.lastName}`}
                size="md"
              />
              <View style={{ flex: 1, gap: 2 }}>
                <Text variant="bodyMedium">
                  Adv. {profile.data.firstName} {profile.data.lastName}
                </Text>
                <Text variant="caption" color="secondary">
                  ₹{Math.round(profile.data.consultationFee).toLocaleString('en-IN')} / Consultation
                </Text>
              </View>
            </View>
          </Card>
        ) : null}

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
                accessibilityState={{ selected }}
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
            <Shimmer height={44} radius={theme.radius.md} />
          </View>
        ) : slots.isError ? (
          <ErrorState message="Could not load slots." onRetry={() => slots.refetch()} />
        ) : (slots.data?.length ?? 0) === 0 ? (
          <EmptyState
            variant="inbox"
            title="No open slots"
            description="Try another date. Weekly hours may still be listed on the profile."
          />
        ) : (
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs }}>
            {slots.data?.map((slot) => {
              const selected = selectedSlot?.startUtc === slot.startUtc;
              return (
                <Pressable
                  key={`${slot.startUtc}-${slot.endUtc}`}
                  accessibilityRole="button"
                  accessibilityState={{ selected }}
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

        {slots.isFetching && !slots.isLoading ? (
          <ActivityIndicator color={theme.colors.accent} />
        ) : null}

        {profile.data ? (
          <Text variant="caption" color="tertiary">
            Consultation fee {`₹${Math.round(profile.data.consultationFee).toLocaleString('en-IN')}`}
            {profile.data.isAvailableForEmergency ? ' · Emergency available' : ''}
          </Text>
        ) : null}
      </ScrollView>
    </Screen>
  );
}
