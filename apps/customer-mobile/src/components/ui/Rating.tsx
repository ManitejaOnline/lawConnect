import React, { useMemo } from 'react';
import { Pressable, View, type StyleProp, type ViewStyle } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type RatingProps = {
  value: number;
  max?: number;
  size?: number;
  interactive?: boolean;
  onChange?: (value: number) => void;
  showValue?: boolean;
  style?: StyleProp<ViewStyle>;
};

export function Rating({
  value,
  max = 5,
  size = 18,
  interactive = false,
  onChange,
  showValue = false,
  style,
}: RatingProps) {
  const theme = useTheme();
  const clamped = Math.max(0, Math.min(max, value));

  const stars = useMemo(() => Array.from({ length: max }, (_, i) => i + 1), [max]);

  return (
    <View style={[{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.xxs }, style]}>
      {stars.map((star) => {
        const filled = clamped >= star;
        const half = !filled && clamped >= star - 0.5;
        const node = (
          <Icon
            key={star}
            name={half ? 'star-half' : 'star'}
            size={size}
            color={filled || half ? theme.colors.rating : theme.colors.borderStrong}
            strokeWidth={1.5}
          />
        );

        if (!interactive) {
          return node;
        }

        return (
          <Pressable
            key={star}
            accessibilityRole="button"
            accessibilityLabel={`Rate ${star}`}
            hitSlop={theme.hitSlop.sm}
            onPress={() => onChange?.(star)}
          >
            {node}
          </Pressable>
        );
      })}
      {showValue ? (
        <Text variant="caption" color="secondary">
          {clamped.toFixed(1)}
        </Text>
      ) : null}
    </View>
  );
}
