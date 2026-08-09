import React, { useMemo } from 'react';
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  View,
  type PressableProps,
  type StyleProp,
  type ViewStyle,
} from 'react-native';
import Animated, { useAnimatedStyle, useSharedValue, withTiming } from 'react-native-reanimated';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'accent' | 'outline' | 'inverse';
type ButtonSize = 'md' | 'lg' | 'sm';

export type ButtonProps = Omit<PressableProps, 'style'> & {
  title: string;
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  fullWidth?: boolean;
  leftSlot?: React.ReactNode;
  rightSlot?: React.ReactNode;
  style?: StyleProp<ViewStyle>;
};

const AnimatedPressable = Animated.createAnimatedComponent(Pressable);

export function Button({
  title,
  variant = 'primary',
  size = 'md',
  loading = false,
  disabled,
  fullWidth = true,
  leftSlot,
  rightSlot,
  style,
  onPressIn,
  onPressOut,
  ...rest
}: ButtonProps) {
  const theme = useTheme();
  const scale = useSharedValue(1);
  const isDisabled = disabled || loading;

  const tones = useMemo(() => {
    switch (variant) {
      case 'secondary':
        return {
          bg: theme.colors.surface,
          fg: theme.colors.textPrimary,
          border: theme.colors.borderStrong,
        };
      case 'outline':
        return {
          bg: theme.colors.surface,
          fg: theme.colors.primary,
          border: theme.colors.primary,
        };
      case 'ghost':
        return {
          bg: 'transparent',
          fg: theme.colors.textPrimary,
          border: 'transparent',
        };
      case 'danger':
        return {
          bg: theme.colors.emergency,
          fg: theme.colors.textInverse,
          border: theme.colors.emergency,
        };
      case 'accent':
      case 'primary':
        return {
          bg: theme.colors.primary,
          fg: theme.colors.textInverse,
          border: theme.colors.primary,
        };
      case 'inverse':
        return {
          bg: 'transparent',
          fg: theme.colors.textInverse,
          border: 'rgba(255,255,255,0.28)',
        };
      default:
        return {
          bg: theme.colors.primary,
          fg: theme.colors.textInverse,
          border: theme.colors.primary,
        };
    }
  }, [theme, variant]);

  const height = size === 'lg' ? 44 : size === 'sm' ? 36 : 40;
  const padH = size === 'sm' ? theme.spacing.sm : theme.spacing.md;
  const fontSize = size === 'sm' ? theme.typography.size.sm : theme.typography.size.md;

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ scale: scale.value }],
  }));

  return (
    <AnimatedPressable
      accessibilityRole="button"
      disabled={isDisabled}
      hitSlop={theme.hitSlop.sm}
      onPressIn={(e) => {
        scale.value = withTiming(0.98, { duration: theme.animation.duration.fast });
        onPressIn?.(e);
      }}
      onPressOut={(e) => {
        scale.value = withTiming(1, { duration: theme.animation.duration.fast });
        onPressOut?.(e);
      }}
      style={[
        styles.base,
        {
          minHeight: height,
          height,
          paddingHorizontal: padH,
          borderRadius: theme.radius.md,
          backgroundColor: tones.bg,
          borderColor: tones.border,
          opacity: isDisabled ? 0.48 : 1,
          alignSelf: fullWidth ? 'stretch' : 'flex-start',
        },
        animatedStyle,
        style,
      ]}
      {...rest}
    >
      {loading ? (
        <ActivityIndicator color={tones.fg} />
      ) : (
        <View style={styles.content}>
          {leftSlot}
          <Text
            variant="bodyMedium"
            style={{
              color: tones.fg,
              fontFamily: theme.typography.fontFamily.bodyMedium,
              fontSize,
              lineHeight: fontSize + 4,
            }}
          >
            {title}
          </Text>
          {rightSlot}
        </View>
      )}
    </AnimatedPressable>
  );
}

const styles = StyleSheet.create({
  base: {
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  content: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
});
