import React from 'react';
import { FlatList, RefreshControl, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Shimmer } from '@/components/feedback/Shimmer';
import { Badge } from '@/components/ui/Badge';
import { Text } from '@/components/ui/Text';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { emergencyApi } from '@/services/api/emergencyApi';
import type { EmergencyStackParamList } from '@/navigation/types';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<EmergencyStackParamList, 'EmergencyHistory'>;

export function EmergencyHistoryScreen({ navigation }: Props) {
  const theme = useTheme();
  const history = useQuery({
    queryKey: ['emergency', 'history'],
    queryFn: () => emergencyApi.getHistory(1, 30),
  });

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title="Emergency history" onBack={() => navigation.goBack()} />
        {history.isLoading ? (
          <View style={{ gap: theme.spacing.sm }}>
            {Array.from({ length: 5 }).map((_, i) => (
              <Shimmer key={i} height={72} radius={theme.radius.lg} />
            ))}
          </View>
        ) : history.isError ? (
          <ErrorState message="Could not load history." onRetry={() => history.refetch()} />
        ) : (
          <FlatList
            data={history.data?.items ?? []}
            keyExtractor={(item) => item.historyId}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'] }}
            refreshControl={
              <RefreshControl
                refreshing={history.isRefetching}
                onRefresh={() => history.refetch()}
                tintColor={theme.colors.accent}
              />
            }
            ListEmptyComponent={
              <EmptyState
                variant="inbox"
                title="No emergencies yet"
                description="Completed SOS sessions will appear here."
              />
            }
            renderItem={({ item }) => (
              <View
                style={{
                  padding: theme.spacing.md,
                  borderRadius: theme.radius.lg,
                  backgroundColor: theme.colors.surface,
                  borderWidth: 1,
                  borderColor: theme.colors.border,
                  gap: theme.spacing.xxs,
                }}
              >
                <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
                  <Text variant="bodyMedium">SOS · {item.finalStatus}</Text>
                  <Badge
                    label={item.finalStatus}
                    tone={item.finalStatus === 'Completed' ? 'success' : 'warning'}
                  />
                </View>
                <Text variant="caption" color="secondary">
                  Attempts {item.attemptCount}
                  {item.responseTimeSeconds != null
                    ? ` · Response ${Math.round(item.responseTimeSeconds)}s`
                    : ''}
                </Text>
                <Text variant="label" color="tertiary">
                  {new Date(item.closedAtUtc).toLocaleString()}
                </Text>
              </View>
            )}
          />
        )}
      </View>
    </Screen>
  );
}
