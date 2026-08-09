import React from 'react';
import {
  Modal as RNModal,
  Pressable,
  StyleSheet,
  View,
  type StyleProp,
  type ViewStyle,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';

export type AppModalProps = {
  visible: boolean;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  contentStyle?: StyleProp<ViewStyle>;
};

/**
 * Centered modal — prefer BottomSheet for non-blocking choices.
 */
export function AppModal({ visible, onClose, title, children, footer, contentStyle }: AppModalProps) {
  const theme = useTheme();
  const insets = useSafeAreaInsets();

  return (
    <RNModal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
      <View style={[styles.overlay, { backgroundColor: theme.colors.overlay, paddingTop: insets.top + 24 }]}>
        <Pressable style={StyleSheet.absoluteFill} onPress={onClose} accessibilityLabel="Dismiss" />
        <View
          style={[
            styles.card,
            theme.elevation.lg,
            {
              backgroundColor: theme.colors.surface,
              borderRadius: theme.radius.xl,
              padding: theme.spacing.lg,
              marginHorizontal: theme.spacing.md,
              maxWidth: theme.layout.maxContentWidth,
              width: '100%',
              alignSelf: 'center',
            },
            contentStyle,
          ]}
        >
          <View style={styles.header}>
            {title ? (
              <Text variant="headline" style={{ flex: 1 }}>
                {title}
              </Text>
            ) : (
              <View style={{ flex: 1 }} />
            )}
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Close"
              hitSlop={theme.hitSlop.md}
              onPress={onClose}
              style={{
                width: theme.touchTarget.min,
                height: theme.touchTarget.min,
                alignItems: 'center',
                justifyContent: 'center',
                borderRadius: theme.radius.full,
                backgroundColor: theme.colors.surfaceMuted,
              }}
            >
              <Icon name="close" size={18} color={theme.colors.textSecondary} />
            </Pressable>
          </View>
          <View style={{ marginTop: theme.spacing.md }}>{children}</View>
          {footer ? <View style={{ marginTop: theme.spacing.lg, gap: theme.spacing.xs }}>{footer}</View> : null}
        </View>
      </View>
    </RNModal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: 'center',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  card: {
    zIndex: 1,
  },
});
