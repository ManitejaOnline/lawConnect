import React, { useEffect } from 'react';
import { Dimensions, Modal, Pressable, StyleSheet, View, type StyleProp, type ViewStyle } from 'react-native';
import { Gesture, GestureDetector } from 'react-native-gesture-handler';
import Animated, {
  runOnJS,
  useAnimatedStyle,
  useSharedValue,
  withSpring,
  withTiming,
} from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type BottomSheetProps = {
  visible: boolean;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
  snapHeight?: number;
  contentStyle?: StyleProp<ViewStyle>;
};

const SCREEN_HEIGHT = Dimensions.get('window').height;

/**
 * Soft bottom sheet with drag-to-dismiss — preferred over intrusive dialogs.
 */
export function BottomSheet({
  visible,
  onClose,
  title,
  children,
  snapHeight = Math.min(SCREEN_HEIGHT * 0.56, 520),
  contentStyle,
}: BottomSheetProps) {
  const theme = useTheme();
  const insets = useSafeAreaInsets();
  const translateY = useSharedValue(snapHeight);
  const overlay = useSharedValue(0);

  useEffect(() => {
    if (visible) {
      overlay.value = withTiming(1, { duration: theme.animation.duration.normal });
      translateY.value = withSpring(0, theme.animation.spring);
    } else {
      overlay.value = withTiming(0, { duration: theme.animation.duration.fast });
      translateY.value = withTiming(snapHeight, { duration: theme.animation.duration.normal });
    }
  }, [visible, snapHeight, theme.animation, overlay, translateY]);

  const sheetStyle = useAnimatedStyle(() => ({
    transform: [{ translateY: translateY.value }],
  }));

  const backdropStyle = useAnimatedStyle(() => ({
    opacity: overlay.value,
  }));

  const pan = Gesture.Pan()
    .onUpdate((e) => {
      translateY.value = Math.max(0, e.translationY);
    })
    .onEnd((e) => {
      if (e.translationY > 120 || e.velocityY > 900) {
        translateY.value = withTiming(snapHeight, { duration: theme.animation.duration.normal }, () => {
          runOnJS(onClose)();
        });
        overlay.value = withTiming(0, { duration: theme.animation.duration.fast });
      } else {
        translateY.value = withSpring(0, theme.animation.spring);
      }
    });

  return (
    <Modal visible={visible} transparent animationType="none" onRequestClose={onClose}>
      <View style={styles.root}>
        <Animated.View style={[styles.backdrop, { backgroundColor: theme.colors.overlay }, backdropStyle]}>
          <Pressable style={StyleSheet.absoluteFill} onPress={onClose} accessibilityLabel="Dismiss sheet" />
        </Animated.View>
        <GestureDetector gesture={pan}>
          <Animated.View
            style={[
              styles.sheet,
              theme.elevation.lg,
              {
                height: snapHeight + insets.bottom,
                paddingBottom: Math.max(insets.bottom, theme.spacing.md),
                backgroundColor: theme.colors.backgroundElevated,
                borderTopLeftRadius: theme.radius['2xl'],
                borderTopRightRadius: theme.radius['2xl'],
                paddingHorizontal: theme.spacing.lg,
              },
              sheetStyle,
              contentStyle,
            ]}
          >
            <View style={styles.handleWrap}>
              <View
                style={{
                  width: 40,
                  height: 4,
                  borderRadius: theme.radius.full,
                  backgroundColor: theme.colors.borderStrong,
                }}
              />
            </View>
            {title ? (
              <Text variant="headline" style={{ marginBottom: theme.spacing.md }}>
                {title}
              </Text>
            ) : null}
            {children}
          </Animated.View>
        </GestureDetector>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    justifyContent: 'flex-end',
  },
  backdrop: {
    ...StyleSheet.absoluteFillObject,
  },
  sheet: {
    width: '100%',
  },
  handleWrap: {
    alignItems: 'center',
    paddingVertical: 12,
  },
});
