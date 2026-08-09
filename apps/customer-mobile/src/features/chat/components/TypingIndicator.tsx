import React from 'react';
import { View } from 'react-native';
import Animated, {
  Easing,
  useAnimatedStyle,
  useSharedValue,
  withDelay,
  withRepeat,
  withSequence,
  withTiming,
} from 'react-native-reanimated';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

function Dot({ delay }: { delay: number }) {
  const theme = useTheme();
  const y = useSharedValue(0);

  React.useEffect(() => {
    y.value = withDelay(
      delay,
      withRepeat(
        withSequence(
          withTiming(-4, { duration: 280, easing: Easing.inOut(Easing.quad) }),
          withTiming(0, { duration: 280, easing: Easing.inOut(Easing.quad) }),
        ),
        -1,
        false,
      ),
    );
  }, [delay, y]);

  const style = useAnimatedStyle(() => ({
    transform: [{ translateY: y.value }],
  }));

  return (
    <Animated.View
      style={[
        {
          width: 6,
          height: 6,
          borderRadius: 3,
          backgroundColor: theme.colors.textTertiary,
          marginHorizontal: 2,
        },
        style,
      ]}
    />
  );
}

export function TypingIndicator({ visible }: { visible: boolean }) {
  const theme = useTheme();
  if (!visible) return null;

  return (
    <View
      accessibilityLabel="Other person is typing"
      style={{
        alignSelf: 'flex-start',
        flexDirection: 'row',
        alignItems: 'center',
        backgroundColor: theme.colors.surfaceMuted,
        borderRadius: theme.radius.lg,
        paddingHorizontal: theme.spacing.sm,
        paddingVertical: theme.spacing.xs,
        marginBottom: theme.spacing.xs,
        gap: theme.spacing.xs,
      }}
    >
      <Dot delay={0} />
      <Dot delay={120} />
      <Dot delay={240} />
      <Text variant="caption" color="tertiary" style={{ marginLeft: 4 }}>
        typing
      </Text>
    </View>
  );
}
