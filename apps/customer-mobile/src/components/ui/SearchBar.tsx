import React from 'react';
import { Pressable, View, type StyleProp, type ViewStyle } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { TextField } from '@/components/ui/TextField';
import { useTheme } from '@/theme/ThemeProvider';

export type SearchBarProps = {
  value: string;
  onChangeText: (value: string) => void;
  placeholder?: string;
  onSubmit?: () => void;
  onClear?: () => void;
  onFilterPress?: () => void;
  style?: StyleProp<ViewStyle>;
};

export function SearchBar({
  value,
  onChangeText,
  placeholder = 'Search',
  onSubmit,
  onClear,
  onFilterPress,
  style,
}: SearchBarProps) {
  const theme = useTheme();

  return (
    <View style={[{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.xs }, style]}>
      <View style={{ flex: 1 }}>
        <TextField
          value={value}
          onChangeText={onChangeText}
          placeholder={placeholder}
          leftIcon="search"
          rightIcon={value ? 'close' : undefined}
          onRightIconPress={value ? () => (onClear ? onClear() : onChangeText('')) : undefined}
          returnKeyType="search"
          onSubmitEditing={onSubmit}
          autoCapitalize="none"
          autoCorrect={false}
        />
      </View>
      {onFilterPress ? (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Filters"
          hitSlop={theme.hitSlop.md}
          onPress={onFilterPress}
          style={{
            width: theme.touchTarget.comfortable,
            height: theme.touchTarget.comfortable,
            borderRadius: theme.radius.md,
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: theme.colors.surfaceMuted,
            borderWidth: 1,
            borderColor: theme.colors.border,
          }}
        >
          <Icon name="filter" size={20} color={theme.colors.textPrimary} />
        </Pressable>
      ) : null}
    </View>
  );
}
