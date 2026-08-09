import React, { useEffect } from 'react';
import { View } from 'react-native';
import Animated, {
  Easing,
  useAnimatedStyle,
  useSharedValue,
  withDelay,
  withRepeat,
  withTiming,
} from 'react-native-reanimated';

import { useTheme } from '@/theme/ThemeProvider';
import { palette } from '@/theme';

function Ripple({ delay, color }: { delay: number; color: string }) {
  const progress = useSharedValue(0);

  useEffect(() => {
    progress.value = withDelay(
      delay,
      withRepeat(
        withTiming(1, { duration: 2200, easing: Easing.out(Easing.quad) }),
        -1,
        false,
      ),
    );
  }, [delay, progress]);

  const style = useAnimatedStyle(() => ({
    opacity: 0.55 * (1 - progress.value),
    transform: [{ scale: 0.35 + progress.value * 1.65 }],
  }));

  return (
    <Animated.View
      style={[
        {
          position: 'absolute',
          width: 160,
          height: 160,
          borderRadius: 80,
          borderWidth: 2,
          borderColor: color,
        },
        style,
      ]}
    />
  );
}

export function RippleSearch({ label = 'SOS' }: { label?: string }) {
  const theme = useTheme();
  const pulse = useSharedValue(1);

  useEffect(() => {
    pulse.value = withRepeat(
      withTiming(1.06, { duration: 900, easing: Easing.inOut(Easing.quad) }),
      -1,
      true,
    );
  }, [pulse]);

  const coreStyle = useAnimatedStyle(() => ({
    transform: [{ scale: pulse.value }],
  }));

  return (
    <View
      accessibilityLabel="Searching for nearby lawyers"
      style={{ width: 220, height: 220, alignItems: 'center', justifyContent: 'center' }}
    >
      <Ripple delay={0} color={theme.colors.danger} />
      <Ripple delay={700} color={theme.colors.danger} />
      <Ripple delay={1400} color={theme.colors.emergency} />
      <Animated.View
        style={[
          {
            width: 88,
            height: 88,
            borderRadius: 44,
            backgroundColor: theme.colors.danger,
            alignItems: 'center',
            justifyContent: 'center',
            ...theme.elevation.lg,
          },
          coreStyle,
        ]}
      >
        <Animated.Text
          style={{
            color: palette.white,
            fontFamily: theme.typography.fontFamily.displayBold,
            fontSize: theme.typography.size.xl,
          }}
        >
          {label}
        </Animated.Text>
      </Animated.View>
    </View>
  );
}
