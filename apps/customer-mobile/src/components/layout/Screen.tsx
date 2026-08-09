import React from 'react';
import {
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  View,
  type StyleProp,
  type ViewStyle,
} from 'react-native';
import { SafeAreaView, type Edge } from 'react-native-safe-area-context';

import { OfflineBanner } from '@/components/feedback/OfflineBanner';
import { useResponsiveLayout } from '@/hooks/useResponsiveLayout';
import { useTheme } from '@/theme/ThemeProvider';

export type ScreenProps = {
  children: React.ReactNode;
  scroll?: boolean;
  keyboard?: boolean;
  footer?: React.ReactNode;
  edges?: Edge[];
  style?: StyleProp<ViewStyle>;
  contentStyle?: StyleProp<ViewStyle>;
  padded?: boolean;
};

export function Screen({
  children,
  scroll = false,
  keyboard = false,
  footer,
  edges = ['top', 'left', 'right'],
  style,
  contentStyle,
  padded = true,
}: ScreenProps) {
  const theme = useTheme();
  const { contentMaxWidth, gutter, isTablet } = useResponsiveLayout();

  const inner = (
    <View
      style={[
        {
          width: '100%',
          maxWidth: contentMaxWidth,
          alignSelf: 'center',
          flexGrow: 1,
          flex: scroll ? undefined : 1,
          paddingHorizontal: padded ? gutter : 0,
          paddingTop: padded ? theme.spacing.md : 0,
          paddingBottom: padded ? theme.spacing.lg : 0,
        },
        contentStyle,
      ]}
    >
      {children}
    </View>
  );

  const body = scroll ? (
    <ScrollView
      style={{ flex: 1 }}
      keyboardShouldPersistTaps="handled"
      keyboardDismissMode="on-drag"
      contentContainerStyle={{
        flexGrow: 1,
        paddingBottom: isTablet ? theme.spacing.xl : theme.spacing.md,
      }}
      showsVerticalScrollIndicator={false}
    >
      {inner}
    </ScrollView>
  ) : (
    inner
  );

  const framed = (
    <>
      <OfflineBanner />
      <View style={{ flex: 1, minHeight: 0 }}>{body}</View>
      {footer ? (
        <View
          style={{
            width: '100%',
            maxWidth: contentMaxWidth,
            alignSelf: 'center',
            paddingHorizontal: padded ? gutter : 0,
            paddingBottom: theme.spacing.sm,
            paddingTop: theme.spacing.xs,
            gap: theme.spacing.xs,
            borderTopWidth: 1,
            borderTopColor: theme.colors.border,
            backgroundColor: theme.colors.background,
          }}
        >
          {footer}
        </View>
      ) : null}
    </>
  );

  const resolvedEdges: Edge[] = footer
    ? Array.from(new Set<Edge>([...edges, 'bottom']))
    : edges;

  return (
    <SafeAreaView
      edges={resolvedEdges}
      style={[{ flex: 1, backgroundColor: theme.colors.background }, style]}
    >
      {keyboard ? (
        <KeyboardAvoidingView
          style={{ flex: 1 }}
          behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
          keyboardVerticalOffset={Platform.OS === 'ios' ? 8 : 0}
        >
          {framed}
        </KeyboardAvoidingView>
      ) : (
        framed
      )}
    </SafeAreaView>
  );
}
