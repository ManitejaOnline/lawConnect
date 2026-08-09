import React from 'react';
import { Image, Pressable, View } from 'react-native';

import { Icon } from '@/components/icons/Icon';
import { Text } from '@/components/ui/Text';
import { ReadReceiptTicks } from '@/features/chat/components/ReadReceiptTicks';
import { useTheme } from '@/theme/ThemeProvider';
import {
  formatChatTime,
  normalizeMessageType,
  type ChatMessage,
} from '@/types/chat';

export type MessageBubbleProps = {
  message: ChatMessage;
  isMine: boolean;
  reaction?: string | null;
  onLongPress?: () => void;
};

export function MessageBubble({ message, isMine, reaction, onLongPress }: MessageBubbleProps) {
  const theme = useTheme();
  const type = normalizeMessageType(message.type);
  const attachment = message.attachments[0];

  const bubbleBg = isMine ? theme.colors.accent : theme.colors.surface;
  const textColor = isMine ? theme.colors.textInverse : theme.colors.textPrimary;
  const metaColor = isMine ? 'rgba(255,255,255,0.72)' : theme.colors.textTertiary;

  return (
    <Pressable
      onLongPress={onLongPress}
      accessibilityRole="button"
      accessibilityLabel={
        type === 'Text'
          ? message.body ?? 'Message'
          : type === 'Image'
            ? 'Image message'
            : 'PDF message'
      }
      style={{
        alignSelf: isMine ? 'flex-end' : 'flex-start',
        maxWidth: '82%',
        marginBottom: theme.spacing.xs,
      }}
    >
      <View
        style={{
          backgroundColor: bubbleBg,
          borderRadius: theme.radius.lg,
          borderBottomRightRadius: isMine ? theme.radius.sm : theme.radius.lg,
          borderBottomLeftRadius: isMine ? theme.radius.lg : theme.radius.sm,
          paddingHorizontal: theme.spacing.sm,
          paddingVertical: theme.spacing.xs + 2,
          borderWidth: isMine ? 0 : 1,
          borderColor: theme.colors.border,
          opacity: message.pending ? 0.7 : 1,
        }}
      >
        {type === 'Image' ? (
          <View style={{ gap: theme.spacing.xs }}>
            {attachment?.downloadUrl ? (
              <Image
                source={{ uri: attachment.downloadUrl }}
                style={{
                  width: 220,
                  height: 160,
                  borderRadius: theme.radius.md,
                  backgroundColor: theme.colors.surfaceMuted,
                }}
                accessibilityLabel={attachment.fileName}
              />
            ) : (
              <View
                style={{
                  width: 220,
                  height: 120,
                  borderRadius: theme.radius.md,
                  backgroundColor: isMine ? 'rgba(255,255,255,0.15)' : theme.colors.surfaceMuted,
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <Icon name="image" size={28} color={textColor} />
              </View>
            )}
            <Text variant="caption" style={{ color: textColor }}>
              {attachment?.fileName ?? 'Photo'}
            </Text>
          </View>
        ) : null}

        {type === 'Pdf' ? (
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm, minWidth: 180 }}>
            <View
              style={{
                width: 40,
                height: 40,
                borderRadius: theme.radius.md,
                backgroundColor: isMine ? 'rgba(255,255,255,0.18)' : theme.colors.dangerMuted,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Icon name="file" size={20} color={isMine ? textColor : theme.colors.danger} />
            </View>
            <View style={{ flex: 1 }}>
              <Text variant="bodyMedium" style={{ color: textColor }} numberOfLines={2}>
                {attachment?.fileName ?? 'Document.pdf'}
              </Text>
              <Text variant="caption" style={{ color: metaColor }}>
                PDF · {Math.max(1, Math.round((attachment?.fileSizeBytes ?? 0) / 1024))} KB
              </Text>
            </View>
          </View>
        ) : null}

        {type === 'Text' && message.body ? (
          <Text variant="body" style={{ color: textColor }}>
            {message.body}
          </Text>
        ) : null}

        <View
          style={{
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'flex-end',
            gap: 4,
            marginTop: 4,
          }}
        >
          <Text variant="label" style={{ color: metaColor, fontSize: 10 }}>
            {formatChatTime(message.sentAtUtc)}
          </Text>
          {isMine ? <ReadReceiptTicks state={message.deliveryState} pending={message.pending} /> : null}
        </View>
      </View>

      {reaction ? (
        <View
          style={{
            alignSelf: isMine ? 'flex-end' : 'flex-start',
            marginTop: -8,
            marginHorizontal: 8,
            backgroundColor: theme.colors.surface,
            borderWidth: 1,
            borderColor: theme.colors.border,
            borderRadius: theme.radius.full,
            paddingHorizontal: 6,
            paddingVertical: 2,
          }}
        >
          <Text variant="body">{reaction}</Text>
        </View>
      ) : null}
    </Pressable>
  );
}
