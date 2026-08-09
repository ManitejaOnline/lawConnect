import React from 'react';
import { View } from 'react-native';

import { StatusBadge } from '@/components/ui/StatusBadge';
import { Card } from '@/components/ui/Card';
import { Text } from '@/components/ui/Text';
import {
  appointmentPartyName,
  formatAppointmentWhen,
  type Appointment,
} from '@/types/appointment';
import { useTheme } from '@/theme/ThemeProvider';

function statusMeta(status: string): { label: string; tone: 'success' | 'warning' | 'danger' | 'neutral' } {
  const value = status.toLowerCase();
  if (value === 'confirmed') return { label: 'Confirmed', tone: 'success' };
  if (value === 'scheduled') return { label: 'Scheduled', tone: 'success' };
  if (value === 'completed') return { label: 'Completed', tone: 'neutral' };
  if (value === 'cancelled') return { label: 'Cancelled', tone: 'danger' };
  return { label: status, tone: 'warning' };
}

export function AppointmentCard({
  item,
  perspective,
  onPress,
}: {
  item: Appointment;
  perspective: 'lawyer' | 'customer';
  onPress?: () => void;
}) {
  const theme = useTheme();
  const name = appointmentPartyName(item, perspective);
  const status = statusMeta(String(item.status));
  const when = formatAppointmentWhen(item.startUtc, item.timeZoneId);

  return (
    <Card onPress={onPress} accessibilityLabel={`Appointment with ${name}`}>
      <View style={{ gap: 4 }}>
        <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: theme.spacing.sm }}>
          <Text variant="bodyMedium" numberOfLines={1} style={{ flex: 1 }}>
            {perspective === 'customer' && !name.startsWith('Adv.') ? `Adv. ${name}` : name}
          </Text>
          <StatusBadge label={status.label} tone={status.tone} />
        </View>
        <Text variant="caption" color="secondary">
          {when}
        </Text>
        <Text variant="caption" color="secondary">
          ₹{Math.round(item.consultationFeeSnapshot).toLocaleString('en-IN')}
          {item.notes ? ` · ${item.notes}` : ''}
        </Text>
      </View>
    </Card>
  );
}
