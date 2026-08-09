import React, { useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  View,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Avatar } from '@/components/ui/Avatar';
import { Text } from '@/components/ui/Text';
import { Icon } from '@/components/icons/Icon';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { ChatComposer } from '@/features/chat/components/ChatComposer';
import { MessageBubble } from '@/features/chat/components/MessageBubble';
import { ReactionPicker } from '@/features/chat/components/ReactionPicker';
import { TypingIndicator } from '@/features/chat/components/TypingIndicator';
import { callUserById } from '@/features/calls/openNativeCall';
import { useChatThread } from '@/features/chat/hooks/useChatThread';
import type { ChatStackParamList } from '@/navigation/types';
import { useChatStore } from '@/store/chatStore';
import { useTheme } from '@/theme/ThemeProvider';
import type { ChatMessage } from '@/types/chat';

type Props = NativeStackScreenProps<ChatStackParamList, 'ChatThread'>;

export function ChatThreadScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const insets = useSafeAreaInsets();
  const {
    conversationId,
    title,
    otherIsOnline,
    otherProfileImageUrl,
  } = route.params;

  const presence = useChatStore((s) => s.presenceByUser[route.params.otherUserId]);
  const online = presence?.isOnline ?? otherIsOnline;

  const {
    messages,
    sending,
    isLoading,
    isError,
    refetch,
    loadOlder,
    hasMore,
    loadingOlder,
    isOtherTyping,
    sendText,
    notifyTyping,
    pickImage,
    pickPdf,
    reactions,
    setReaction,
    userId,
  } = useChatThread(conversationId);

  const [reactionTarget, setReactionTarget] = useState<string | null>(null);

  // FlatList inverted: show newest at bottom by reversing for inverted list
  const invertedData = [...messages].reverse();

  return (
    <KeyboardAvoidingView
      style={{ flex: 1, backgroundColor: theme.colors.background, paddingTop: insets.top }}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      keyboardVerticalOffset={0}
    >
      <View
        style={{
          flexDirection: 'row',
          alignItems: 'center',
          gap: theme.spacing.sm,
          paddingHorizontal: theme.layout.gutter,
          paddingVertical: theme.spacing.sm,
          borderBottomWidth: 1,
          borderBottomColor: theme.colors.divider,
          backgroundColor: theme.colors.backgroundElevated,
        }}
      >
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Go back"
          onPress={() => navigation.goBack()}
          hitSlop={theme.hitSlop.md}
          style={{
            width: theme.touchTarget.min,
            height: theme.touchTarget.min,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Icon name="chevron-left" size={24} color={theme.colors.textPrimary} />
        </Pressable>
        <Avatar uri={otherProfileImageUrl} name={title} size="md" />
        <View style={{ flex: 1 }}>
          <Text variant="bodyMedium" numberOfLines={1}>
            {title}
          </Text>
          <Text variant="caption" color={online ? 'accent' : 'tertiary'}>
            {isOtherTyping ? 'typing…' : online ? 'Online' : 'Offline'}
          </Text>
        </View>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Call"
          onPress={() => void callUserById(route.params.otherUserId)}
          style={{
            width: 40,
            height: 40,
            borderRadius: 20,
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: theme.colors.surfaceMuted,
          }}
        >
          <Icon name="phone" size={18} color={theme.colors.accent} />
        </Pressable>
      </View>

      {isLoading && !messages.length ? (
        <View style={{ flex: 1, padding: theme.layout.gutter, gap: theme.spacing.sm }}>
          {Array.from({ length: 8 }).map((_, i) => (
            <Shimmer
              key={i}
              height={44}
              radius={theme.radius.lg}
              style={{ alignSelf: i % 2 ? 'flex-end' : 'flex-start', width: '70%' }}
            />
          ))}
        </View>
      ) : isError && !messages.length ? (
        <ErrorState message="Could not load messages." onRetry={() => refetch()} />
      ) : (
        <FlatList
          data={invertedData}
          inverted
          keyExtractor={(item) => item.messageId}
          contentContainerStyle={{
            paddingHorizontal: theme.layout.gutter,
            paddingVertical: theme.spacing.sm,
            flexGrow: 1,
          }}
          onEndReached={() => {
            if (hasMore) void loadOlder();
          }}
          onEndReachedThreshold={0.2}
          ListFooterComponent={
            loadingOlder ? (
              <ActivityIndicator color={theme.colors.accent} style={{ marginVertical: 12 }} />
            ) : null
          }
          ListHeaderComponent={<TypingIndicator visible={isOtherTyping} />}
          renderItem={({ item }: { item: ChatMessage }) => (
            <MessageBubble
              message={item}
              isMine={item.senderUserId === userId}
              reaction={reactions[item.messageId]}
              onLongPress={() => setReactionTarget(item.messageId)}
            />
          )}
        />
      )}

      <ReactionPicker
        visible={Boolean(reactionTarget)}
        onSelect={(emoji) => {
          if (reactionTarget) setReaction(reactionTarget, emoji);
          setReactionTarget(null);
        }}
        onClear={() => {
          if (reactionTarget) setReaction(reactionTarget, null);
          setReactionTarget(null);
        }}
      />

      <View style={{ paddingBottom: Math.max(insets.bottom, 8) }}>
        <ChatComposer
          sending={sending}
          onSend={sendText}
          onChangeDraft={notifyTyping}
          onPickImage={pickImage}
          onPickPdf={pickPdf}
        />
      </View>
    </KeyboardAvoidingView>
  );
}
