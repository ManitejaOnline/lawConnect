import React, { useEffect } from 'react';
import { ActivityIndicator, View } from 'react-native';
import Animated, {
  FadeIn,
  FadeOut,
  useAnimatedStyle,
  useSharedValue,
  withTiming,
} from 'react-native-reanimated';

import { BrandMark } from '@/components/brand/BrandMark';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

type SplashScreenProps = {
  statusLabel?: string;
};

export function SplashScreen({ statusLabel = 'Starting…' }: SplashScreenProps) {
  const theme = useTheme();
  const progress = useSharedValue(0);

  useEffect(() => {
    progress.value = withTiming(1, { duration: theme.animation.duration.entrance });
  }, [progress, theme.animation.duration.entrance]);

  const markStyle = useAnimatedStyle(() => ({
    opacity: progress.value,
    transform: [{ scale: 0.92 + progress.value * 0.08 }],
  }));

  return (
    <View
      accessibilityLabel="LawConnect splash screen"
      style={{
        flex: 1,
        backgroundColor: theme.colors.background,
        alignItems: 'center',
        justifyContent: 'center',
        padding: theme.spacing.xl,
      }}
    >
      <Animated.View
        entering={FadeIn.duration(theme.animation.duration.entrance)}
        style={[{ alignItems: 'center' }, markStyle]}
      >
        <BrandMark size={76} />
        <Text variant="body" color="secondary" align="center" style={{ marginTop: theme.spacing.sm }}>
          Legal help, on demand
        </Text>
      </Animated.View>

      <Animated.View
        entering={FadeIn.delay(280)}
        exiting={FadeOut}
        style={{
          position: 'absolute',
          bottom: theme.spacing['4xl'],
          alignItems: 'center',
          gap: theme.spacing.sm,
        }}
      >
        <ActivityIndicator color={theme.colors.primary} />
        <Text variant="caption" color="secondary">
          {statusLabel}
        </Text>
      </Animated.View>
    </View>
  );
}
