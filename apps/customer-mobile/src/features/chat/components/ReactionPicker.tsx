import React from 'react';
import { Pressable, View } from 'react-native';

import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';
import { CHAT_REACTION_EMOJIS } from '@/types/chat';

export function ReactionPicker({
  visible,
  onSelect,
  onClear,
}: {
  visible: boolean;
  onSelect: (emoji: string) => void;
  onClear: () => void;
}) {
  const theme = useTheme();
  if (!visible) return null;

  return (
    <View
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.xs,
        alignSelf: 'center',
        backgroundColor: theme.colors.surface,
        borderRadius: theme.radius.full,
        borderWidth: 1,
        borderColor: theme.colors.border,
        paddingHorizontal: theme.spacing.sm,
        paddingVertical: theme.spacing.xs,
        marginBottom: theme.spacing.sm,
        ...theme.elevation.sm,
      }}
    >
      {CHAT_REACTION_EMOJIS.map((emoji) => (
        <Pressable
          key={emoji}
          accessibilityRole="button"
          accessibilityLabel={`React ${emoji}`}
          onPress={() => onSelect(emoji)}
          hitSlop={8}
          style={{ padding: 4 }}
        >
          <Text variant="title">{emoji}</Text>
        </Pressable>
      ))}
      <Pressable accessibilityRole="button" accessibilityLabel="Clear reaction" onPress={onClear}>
        <Text variant="caption" color="secondary">
          Clear
        </Text>
      </Pressable>
    </View>
  );
}
