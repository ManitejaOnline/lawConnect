import React, { useEffect } from 'react';
import { View } from 'react-native';
import Animated, {
  Easing,
  useAnimatedProps,
  useSharedValue,
  withTiming,
} from 'react-native-reanimated';
import Svg, { Circle } from 'react-native-svg';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

const AnimatedCircle = Animated.createAnimatedComponent(Circle);

export function CountdownRing({
  remainingSeconds,
  totalSeconds = 20,
}: {
  remainingSeconds: number;
  totalSeconds?: number;
}) {
  const theme = useTheme();
  const size = 120;
  const stroke = 8;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const progress = useSharedValue(1);

  useEffect(() => {
    const ratio = Math.max(0, Math.min(1, remainingSeconds / Math.max(totalSeconds, 1)));
    progress.value = withTiming(ratio, { duration: 350, easing: Easing.out(Easing.quad) });
  }, [progress, remainingSeconds, totalSeconds]);

  const animatedProps = useAnimatedProps(() => ({
    strokeDashoffset: circumference * (1 - progress.value),
  }));

  return (
    <View
      accessibilityLabel={`${remainingSeconds} seconds remaining`}
      style={{ width: size, height: size, alignItems: 'center', justifyContent: 'center' }}
    >
      <Svg width={size} height={size} style={{ position: 'absolute' }}>
        <Circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke={theme.colors.borderStrong}
          strokeWidth={stroke}
          fill="none"
        />
        <AnimatedCircle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke={theme.colors.danger}
          strokeWidth={stroke}
          fill="none"
          strokeDasharray={`${circumference} ${circumference}`}
          animatedProps={animatedProps}
          strokeLinecap="round"
          rotation="-90"
          origin={`${size / 2}, ${size / 2}`}
        />
      </Svg>
      <Text variant="title">{remainingSeconds}s</Text>
    </View>
  );
}
