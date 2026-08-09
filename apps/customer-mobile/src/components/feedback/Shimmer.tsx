import React, { useEffect } from 'react';
import { StyleSheet, View, type StyleProp, type ViewStyle } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import Animated, {
  Easing,
  useAnimatedStyle,
  useSharedValue,
  withRepeat,
  withTiming,
} from 'react-native-reanimated';

import { useTheme } from '@/theme/ThemeProvider';

const AnimatedGradient = Animated.createAnimatedComponent(LinearGradient);

export type ShimmerProps = {
  width?: number | `${number}%`;
  height?: number;
  radius?: number;
  style?: StyleProp<ViewStyle>;
};

/** Soft horizontal shimmer for skeleton placeholders. */
export function Shimmer({ width = '100%', height = 16, radius, style }: ShimmerProps) {
  const theme = useTheme();
  const progress = useSharedValue(0);

  useEffect(() => {
    progress.value = withRepeat(
      withTiming(1, { duration: 1400, easing: Easing.inOut(Easing.quad) }),
      -1,
      false,
    );
  }, [progress]);

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ translateX: -80 + progress.value * 220 }],
  }));

  return (
    <View
      style={[
        {
          width,
          height,
          borderRadius: radius ?? theme.radius.md,
          backgroundColor: theme.colors.skeleton,
          overflow: 'hidden',
        },
        style,
      ]}
    >
      <AnimatedGradient
        colors={[theme.colors.skeleton, theme.colors.skeletonHighlight, theme.colors.skeleton]}
        start={{ x: 0, y: 0.5 }}
        end={{ x: 1, y: 0.5 }}
        style={[StyleSheet.absoluteFill, { width: 120 }, animatedStyle]}
      />
    </View>
  );
}
