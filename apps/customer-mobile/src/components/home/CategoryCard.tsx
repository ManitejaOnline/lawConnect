import React from 'react';
import { Pressable, View, type StyleProp, type ViewStyle } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';
import type { PracticeAreaCategory } from '@/types/home';

export type CategoryCardProps = {
  category: PracticeAreaCategory;
  onPress?: () => void;
  style?: StyleProp<ViewStyle>;
};

export function CategoryCard({ category, onPress, style }: CategoryCardProps) {
  const theme = useTheme();

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={category.name}
      onPress={onPress}
      style={({ pressed }) => [
        {
          width: 72,
          alignItems: 'center',
          gap: 6,
          opacity: pressed ? 0.88 : 1,
        },
        style,
      ]}
    >
      <View
        style={{
          width: 48,
          height: 48,
          borderRadius: 24,
          backgroundColor: theme.colors.accentSoft,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Icon name={category.icon} size={20} color={theme.colors.primary} />
      </View>
      <Text variant="caption" numberOfLines={1} align="center">
        {category.shortName}
      </Text>
    </Pressable>
  );
}
