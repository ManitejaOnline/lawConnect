import React, { useEffect } from 'react';
import { Pressable, View } from 'react-native';
import Animated, { FadeInUp, FadeOutUp } from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';
import { useToastStore, type ToastItem, type ToastVariant } from '@/store/toastStore';

function toneColors(variant: ToastVariant, theme: ReturnType<typeof useTheme>) {
  switch (variant) {
    case 'success':
      return { bg: theme.colors.successMuted, fg: theme.colors.success, icon: 'check' as const };
    case 'warning':
      return { bg: theme.colors.warningMuted, fg: theme.colors.warning, icon: 'alert' as const };
    case 'error':
      return { bg: theme.colors.dangerMuted, fg: theme.colors.danger, icon: 'alert' as const };
    default:
      return { bg: theme.colors.surface, fg: theme.colors.accent, icon: 'bell' as const };
  }
}

function ToastCard({ item }: { item: ToastItem }) {
  const theme = useTheme();
  const dismiss = useToastStore((s) => s.dismiss);
  const tone = toneColors(item.variant, theme);

  useEffect(() => {
    const id = setTimeout(() => dismiss(item.id), item.durationMs);
    return () => clearTimeout(id);
  }, [dismiss, item.durationMs, item.id]);

  return (
    <Animated.View
      entering={FadeInUp.duration(theme.animation.duration.normal)}
      exiting={FadeOutUp.duration(theme.animation.duration.fast)}
      style={[
        theme.elevation.md,
        {
          backgroundColor: theme.colors.backgroundElevated,
          borderRadius: theme.radius.lg,
          borderWidth: 1,
          borderColor: theme.colors.border,
          padding: theme.spacing.md,
          flexDirection: 'row',
          gap: theme.spacing.sm,
          alignItems: 'flex-start',
        },
      ]}
    >
      <View
        style={{
          width: 32,
          height: 32,
          borderRadius: theme.radius.full,
          backgroundColor: tone.bg,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Icon name={tone.icon} size={16} color={tone.fg} />
      </View>
      <View style={{ flex: 1, gap: 2 }}>
        <Text variant="bodyMedium">{item.title}</Text>
        {item.message ? (
          <Text variant="caption" color="secondary">
            {item.message}
          </Text>
        ) : null}
      </View>
      <Pressable accessibilityRole="button" hitSlop={theme.hitSlop.md} onPress={() => dismiss(item.id)}>
        <Icon name="close" size={16} color={theme.colors.textTertiary} />
      </Pressable>
    </Animated.View>
  );
}

export function ToastHost() {
  const insets = useSafeAreaInsets();
  const theme = useTheme();
  const toasts = useToastStore((s) => s.toasts);

  return (
    <View
      pointerEvents="box-none"
      style={{
        position: 'absolute',
        top: insets.top + theme.spacing.xs,
        left: theme.spacing.md,
        right: theme.spacing.md,
        gap: theme.spacing.xs,
        zIndex: 1000,
      }}
    >
      {toasts.map((toast) => (
        <ToastCard key={toast.id} item={toast} />
      ))}
    </View>
  );
}
