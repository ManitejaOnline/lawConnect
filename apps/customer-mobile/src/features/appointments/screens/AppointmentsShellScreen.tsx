import React from 'react';

import { EmptyState } from '@/components/feedback/EmptyState';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';

/** Shell placeholder — no business features. */
export function AppointmentsShellScreen() {
  return (
    <Screen scroll>
      <ScreenHeader title="Bookings" subtitle="Upcoming & past" />
      <EmptyState
        variant="inbox"
        title="No bookings yet"
        description="Appointment management will wire to /api/appointments next."
      />
    </Screen>
  );
}
