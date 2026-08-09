import React, { useMemo, useState } from 'react';
import { RefreshControl, ScrollView, View } from 'react-native';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { LoadingState } from '@/components/feedback/LoadingState';
import { Chip } from '@/components/ui/Chip';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { AppointmentCard } from '@/features/appointments/components/AppointmentCard';
import {
  useAppointmentHistoryQuery,
  useUpcomingAppointmentsQuery,
} from '@/features/appointments/hooks/useAppointmentQueries';
import {
  isCancelled,
  isPastOrCompleted,
  isUpcomingScheduled,
} from '@/types/appointment';
import { useTheme } from '@/theme/ThemeProvider';

type TabKey = 'upcoming' | 'completed' | 'cancelled';

const TABS: { key: TabKey; label: string }[] = [
  { key: 'upcoming', label: 'Upcoming' },
  { key: 'completed', label: 'Completed' },
  { key: 'cancelled', label: 'Cancelled' },
];

export function AppointmentsListScreen({
  perspective,
  onOpen,
}: {
  perspective: 'lawyer' | 'customer';
  onOpen: (appointmentId: string) => void;
}) {
  const theme = useTheme();
  const [tab, setTab] = useState<TabKey>('upcoming');
  const upcoming = useUpcomingAppointmentsQuery();
  const history = useAppointmentHistoryQuery();

  const items = useMemo(() => {
    const upcomingItems = upcoming.data?.items ?? [];
    const historyItems = history.data?.items ?? [];
    if (tab === 'upcoming') return upcomingItems.filter((row) => isUpcomingScheduled(row));
    if (tab === 'cancelled') return historyItems.filter(isCancelled);
    return historyItems.filter((row) => isPastOrCompleted(row));
  }, [history.data?.items, tab, upcoming.data?.items]);

  const loading = (upcoming.isLoading || history.isLoading) && !upcoming.data && !history.data;
  const errored = (upcoming.isError || history.isError) && !upcoming.data && !history.data;
  const refresh = () => {
    void upcoming.refetch();
    void history.refetch();
  };

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter }}>
        <ScreenHeader
          title="Appointments"
          subtitle={perspective === 'lawyer' ? 'Your client bookings' : 'Your consultations'}
        />
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs, marginBottom: theme.spacing.md }}>
          {TABS.map((entry) => (
            <Chip
              key={entry.key}
              label={entry.label}
              selected={tab === entry.key}
              onPress={() => setTab(entry.key)}
            />
          ))}
        </View>
      </View>

      {loading ? (
        <LoadingState label="Loading appointments" fullScreen />
      ) : errored ? (
        <ErrorState message="Could not load appointments." onRetry={refresh} />
      ) : (
        <ScrollView
          refreshControl={
            <RefreshControl refreshing={upcoming.isRefetching || history.isRefetching} onRefresh={refresh} />
          }
          contentContainerStyle={{
            paddingHorizontal: theme.layout.gutter,
            paddingBottom: theme.spacing['3xl'],
            gap: theme.spacing.sm,
            flexGrow: 1,
          }}
        >
          {items.length === 0 ? (
            <EmptyState
              variant="inbox"
              title="No appointments"
              description={
                perspective === 'lawyer'
                  ? 'When a customer books you, the appointment appears here automatically.'
                  : 'Book a consultation from a lawyer profile to see it here.'
              }
            />
          ) : (
            items.map((item) => (
              <AppointmentCard
                key={item.id}
                item={item}
                perspective={perspective}
                onPress={() => onOpen(item.id)}
              />
            ))
          )}
        </ScrollView>
      )}
    </Screen>
  );
}
