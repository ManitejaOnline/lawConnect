import React from 'react';
import { FlatList, Pressable, RefreshControl, View } from 'react-native';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Badge } from '@/components/ui/Badge';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { openLawyerEmergency, parseEmergencyNotificationData } from '@/features/emergency/deepLink';
import { homeQueryKeys, useNotificationsQuery } from '@/features/home/hooks/useHomeQueries';
import { notificationsApi } from '@/services/api/notificationsApi';
import type { HomeStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<HomeStackParamList, 'Notifications'>;

export function NotificationsScreen({ navigation }: Props) {
  const theme = useTheme();
  const queryClient = useQueryClient();
  const notifications = useNotificationsQuery();

  const markRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: homeQueryKeys.notifications() });
    },
  });

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title="Notifications" subtitle="Stay up to date" onBack={() => navigation.goBack()} />

        {notifications.isLoading ? (
          <View style={{ gap: theme.spacing.sm }}>
            {Array.from({ length: 6 }).map((_, i) => (
              <Shimmer key={i} height={72} radius={theme.radius.lg} />
            ))}
          </View>
        ) : notifications.isError ? (
          <ErrorState message="Could not load notifications." onRetry={() => notifications.refetch()} />
        ) : (
          <FlatList
            data={notifications.data?.items ?? []}
            keyExtractor={(item) => item.id}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'] }}
            refreshControl={
              <RefreshControl
                refreshing={notifications.isRefetching}
                onRefresh={() => notifications.refetch()}
                tintColor={theme.colors.accent}
              />
            }
            ListEmptyComponent={
              <EmptyState variant="inbox" title="All caught up" description="New alerts will show up here." />
            }
            renderItem={({ item }) => (
              <Pressable
                accessibilityRole="button"
                onPress={() => {
                  if (!item.isRead) markRead.mutate(item.id);
                  const target = parseEmergencyNotificationData(item.dataJson);
                  if (target) openLawyerEmergency(target);
                }}
                style={{
                  padding: theme.spacing.md,
                  borderRadius: theme.radius.lg,
                  backgroundColor: item.isRead ? theme.colors.surface : theme.colors.accentSoft,
                  borderWidth: 1,
                  borderColor: theme.colors.border,
                  gap: theme.spacing.xxs,
                  ...theme.elevation.sm,
                }}
              >
                <View style={{ flexDirection: 'row', justifyContent: 'space-between', gap: theme.spacing.sm }}>
                  <Text variant="bodyMedium" style={{ flex: 1 }}>
                    {item.title}
                  </Text>
                  {!item.isRead ? <Badge label="New" tone="accent" /> : null}
                </View>
                <Text variant="caption" color="secondary">
                  {item.body}
                </Text>
                <Text variant="label" color="tertiary">
                  {new Date(item.createdAt).toLocaleString()}
                </Text>
              </Pressable>
            )}
          />
        )}
      </View>
    </Screen>
  );
}
