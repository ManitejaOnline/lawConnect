import React from 'react';
import { View, type StyleProp, type ViewStyle } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Avatar } from '@/components/ui/Avatar';
import { Badge } from '@/components/ui/Badge';
import { Card } from '@/components/ui/Card';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';
import type { LawyerSearchItem } from '@/types/home';

export type LawyerCardProps = {
  lawyer: LawyerSearchItem;
  onPress?: () => void;
  compact?: boolean;
  style?: StyleProp<ViewStyle>;
};

function formatFee(value: number) {
  return `₹${Math.round(value).toLocaleString('en-IN')}`;
}

export function LawyerCard({ lawyer, onPress, compact = false, style }: LawyerCardProps) {
  const theme = useTheme();
  const name = `${lawyer.firstName} ${lawyer.lastName}`.trim();
  const area = lawyer.practiceAreas?.[0] || 'General practice';
  const rating = lawyer.ratingAverage || 0;

  return (
    <Card
      onPress={onPress}
      padded
      style={[{ minWidth: compact ? 240 : undefined }, style]}
      accessibilityLabel={`${name}, lawyer`}
    >
      <View style={{ flexDirection: 'row', gap: theme.spacing.sm, alignItems: 'center' }}>
        <Avatar uri={lawyer.profileImageUrl} name={name} size={compact ? 'md' : 'lg'} />
        <View style={{ flex: 1, gap: 2 }}>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
            <Text variant="bodyMedium" numberOfLines={1} style={{ flexShrink: 1 }}>
              {name.startsWith('Adv.') ? name : `Adv. ${name}`}
            </Text>
            {lawyer.isVerified ? <Badge label="Verified" tone="success" /> : null}
          </View>
          <Text variant="caption" color="secondary" numberOfLines={1}>
            {area} · {lawyer.yearsOfExperience}+ years
          </Text>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 4 }}>
            <Icon name="star" size={12} color={theme.colors.rating} />
            <Text variant="caption">
              {rating.toFixed(1)}
              <Text variant="caption" color="tertiary">
                {' '}
                ({lawyer.ratingCount})
              </Text>
            </Text>
          </View>
          <Text variant="bodyMedium" color="accent">
            {formatFee(lawyer.consultationFee)}
            <Text variant="caption" color="secondary">
              {' '}
              / Consultation
            </Text>
          </Text>
        </View>
      </View>
    </Card>
  );
}
