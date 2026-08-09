import React from 'react';
import { FlatList, RefreshControl, View } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { SearchBar } from '@/components/ui/SearchBar';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { ConversationRow } from '@/features/chat/components/ConversationRow';
import {
  useChatInboxRealtime,
  useConversationSearchState,
  useFilteredConversations,
} from '@/features/chat/hooks/useConversations';
import type { ChatStackParamList } from '@/navigation/types';
import { useAuthStore } from '@/store/authStore';
import { useTheme } from '@/theme/ThemeProvider';

type Nav = NativeStackNavigationProp<ChatStackParamList, 'Conversations'>;

export function ConversationsScreen() {
  const theme = useTheme();
  const navigation = useNavigation<Nav>();
  const { search, setSearch, clear } = useConversationSearchState();
  const { filtered, query, refetch } = useFilteredConversations(search);
  const intendedAudience = useAuthStore((s) => s.intendedAudience);
  useChatInboxRealtime();

  return (
    <Screen edges={['top', 'left', 'right']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter }}>
        <ScreenHeader title="Messages" subtitle="Secure conversations" />
        <SearchBar
          value={search}
          onChangeText={setSearch}
          onClear={clear}
          placeholder="Search conversations"
          style={{ marginBottom: theme.spacing.sm }}
        />
      </View>

      {query.isLoading && !filtered.length ? (
        <View style={{ paddingHorizontal: theme.layout.gutter, gap: theme.spacing.sm }}>
          {Array.from({ length: 6 }).map((_, i) => (
            <Shimmer key={i} height={64} radius={theme.radius.lg} />
          ))}
        </View>
      ) : query.isError && !filtered.length ? (
        <ErrorState message="Could not load conversations." onRetry={() => refetch()} />
      ) : (
        <FlatList
          data={filtered}
          keyExtractor={(item) => item.conversationId}
          refreshControl={
            <RefreshControl
              refreshing={query.isRefetching}
              onRefresh={() => refetch()}
              tintColor={theme.colors.accent}
            />
          }
          ItemSeparatorComponent={() => (
            <View
              style={{
                height: 1,
                backgroundColor: theme.colors.divider,
                marginLeft: theme.layout.gutter + 64,
              }}
            />
          )}
          ListEmptyComponent={
            <EmptyState
              variant="inbox"
              title={search ? 'No matches' : 'Inbox is empty'}
              description={
                search
                  ? 'Try a different name or message preview.'
                  : intendedAudience === 'lawyer'
                    ? 'When a customer messages you, the conversation appears here in realtime.'
                    : 'Start a conversation from a lawyer profile. Messages sync in realtime.'
              }
            />
          }
          renderItem={({ item }) => (
            <ConversationRow
              item={item}
              onPress={() =>
                navigation.navigate('ChatThread', {
                  conversationId: item.conversationId,
                  title: `${item.otherFirstName} ${item.otherLastName}`.trim(),
                  otherUserId: item.otherUserId,
                  otherIsOnline: item.otherIsOnline,
                  otherProfileImageUrl: item.otherProfileImageUrl,
                })
              }
            />
          )}
          contentContainerStyle={{ paddingBottom: theme.spacing['3xl'], flexGrow: 1 }}
        />
      )}
    </Screen>
  );
}
