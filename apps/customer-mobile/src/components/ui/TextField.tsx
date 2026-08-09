import React, { useMemo, useState } from 'react';
import {
  Pressable,
  StyleSheet,
  TextInput,
  View,
  type StyleProp,
  type TextInputProps,
  type ViewStyle,
} from 'react-native';

import { Icon, type IconName } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type TextFieldProps = Omit<TextInputProps, 'style'> & {
  label?: string;
  hint?: string;
  error?: string;
  leftIcon?: IconName;
  rightIcon?: IconName;
  onRightIconPress?: () => void;
  containerStyle?: StyleProp<ViewStyle>;
};

export function TextField({
  label,
  hint,
  error,
  leftIcon,
  rightIcon,
  onRightIconPress,
  containerStyle,
  editable = true,
  onFocus,
  onBlur,
  ...rest
}: TextFieldProps) {
  const theme = useTheme();
  const [focused, setFocused] = useState(false);
  const hasError = Boolean(error);

  const borderColor = useMemo(() => {
    if (hasError) return theme.colors.danger;
    if (focused) return theme.colors.focusRing;
    return theme.colors.borderStrong;
  }, [focused, hasError, theme.colors]);

  return (
    <View style={[{ gap: theme.spacing.xs }, containerStyle]}>
      {label ? (
        <Text variant="label" color="secondary">
          {label}
        </Text>
      ) : null}
      <View
        style={[
          styles.field,
          {
            minHeight: theme.touchTarget.comfortable,
            borderRadius: theme.radius.md,
            borderColor,
            backgroundColor: theme.colors.surface,
            paddingHorizontal: theme.spacing.md,
            gap: theme.spacing.xs,
          },
        ]}
      >
        {leftIcon ? <Icon name={leftIcon} size={20} color={theme.colors.textTertiary} /> : null}
        <TextInput
          {...rest}
          editable={editable}
          placeholderTextColor={theme.colors.textTertiary}
          onFocus={(e) => {
            setFocused(true);
            onFocus?.(e);
          }}
          onBlur={(e) => {
            setFocused(false);
            onBlur?.(e);
          }}
          style={{
            flex: 1,
            color: theme.colors.textPrimary,
            fontFamily: theme.typography.fontFamily.body,
            fontSize: theme.typography.size.md,
            paddingVertical: theme.spacing.sm,
          }}
        />
        {rightIcon ? (
          <Pressable
            accessibilityRole="button"
            disabled={!onRightIconPress}
            hitSlop={theme.hitSlop.md}
            onPress={onRightIconPress}
          >
            <Icon name={rightIcon} size={20} color={theme.colors.textTertiary} />
          </Pressable>
        ) : null}
      </View>
      {hasError ? (
        <Text variant="caption" color="danger">
          {error}
        </Text>
      ) : hint ? (
        <Text variant="caption" color="tertiary">
          {hint}
        </Text>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  field: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1.5,
  },
});
