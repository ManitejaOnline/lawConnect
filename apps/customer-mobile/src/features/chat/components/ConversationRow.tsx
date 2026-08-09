import React from 'react';
import { Pressable, View } from 'react-native';

import { Avatar } from '@/components/ui/Avatar';
import { Text } from '@/components/ui/Text';
import { useTheme } from '@/theme/ThemeProvider';
import {
  conversationDisplayName,
  formatChatTime,
  type ConversationListItem,
} from '@/types/chat';

export function ConversationRow({
  item,
  onPress,
}: {
  item: ConversationListItem;
  onPress: () => void;
}) {
  const theme = useTheme();
  const name = conversationDisplayName(item);

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${name}${item.unreadCount ? `, ${item.unreadCount} unread` : ''}`}
      onPress={onPress}
      style={({ pressed }) => ({
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.sm,
        paddingVertical: theme.spacing.sm,
        paddingHorizontal: theme.layout.gutter,
        backgroundColor: pressed ? theme.colors.surfaceMuted : 'transparent',
        minHeight: 60,
      })}
    >
      <View>
        <Avatar uri={item.otherProfileImageUrl} name={name} size="md" />
        {item.otherIsOnline ? (
          <View
            style={{
              position: 'absolute',
              right: 2,
              bottom: 2,
              width: 12,
              height: 12,
              borderRadius: 6,
              backgroundColor: theme.colors.success,
              borderWidth: 2,
              borderColor: theme.colors.background,
            }}
          />
        ) : null}
      </View>

      <View style={{ flex: 1, gap: 2 }}>
        <View style={{ flexDirection: 'row', justifyContent: 'space-between', gap: theme.spacing.sm }}>
          <Text variant="bodyMedium" numberOfLines={1} style={{ flex: 1 }}>
            {name}
          </Text>
          <Text variant="caption" color="tertiary">
            {formatChatTime(item.lastMessageAtUtc)}
          </Text>
        </View>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.xs }}>
          <Text
            variant="caption"
            color={item.unreadCount > 0 ? 'primary' : 'secondary'}
            numberOfLines={1}
            style={{ flex: 1, fontWeight: item.unreadCount > 0 ? '600' : undefined }}
          >
            {item.lastMessagePreview || 'No messages yet'}
          </Text>
          {item.unreadCount > 0 ? (
            <View
              style={{
                minWidth: 18,
                height: 18,
                borderRadius: 9,
                paddingHorizontal: 5,
                backgroundColor: theme.colors.primary,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Text variant="label" color="inverse" style={{ fontSize: 10 }}>
                {item.unreadCount > 99 ? '99+' : item.unreadCount}
              </Text>
            </View>
          ) : null}
        </View>
      </View>
    </Pressable>
  );
}
