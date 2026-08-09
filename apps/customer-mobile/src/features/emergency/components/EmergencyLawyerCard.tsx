import React from 'react';
import { View } from 'react-native';

import { Avatar } from '@/components/ui/Avatar';
import { Badge } from '@/components/ui/Badge';
import { Card } from '@/components/ui/Card';
import { Rating } from '@/components/ui/Rating';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';
import type { LawyerProfile } from '@/types/home';

export type EmergencyLawyerCardProps = {
  lawyer: LawyerProfile;
  distanceKm?: number | null;
  etaMinutes?: number | null;
};

export function EmergencyLawyerCard({ lawyer }: EmergencyLawyerCardProps) {
  const theme = useTheme();
  const name = `${lawyer.firstName} ${lawyer.lastName}`.trim();
  const specialization =
    lawyer.practiceAreas.find((a) => a.isPrimary)?.name || lawyer.practiceAreas[0]?.name;

  return (
    <Card accessibilityLabel={`Assigned lawyer ${name}`}>
      <View style={{ flexDirection: 'row', gap: theme.spacing.md }}>
        <Avatar uri={lawyer.profileImageUrl} name={name} size="xl" />
        <View style={{ flex: 1, gap: theme.spacing.xxs }}>
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.xs, alignItems: 'center' }}>
            <Text variant="headline" style={{ flexShrink: 1 }}>
              {name}
            </Text>
            {lawyer.isVerified ? <Badge label="Verified" tone="accent" /> : null}
          </View>
          {specialization ? (
            <Text variant="body" color="secondary">
              {specialization}
            </Text>
          ) : null}
          <Rating value={lawyer.ratingAverage} showValue size={16} />
        </View>
      </View>
    </Card>
  );
}
