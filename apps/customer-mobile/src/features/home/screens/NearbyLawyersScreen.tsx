import React from 'react';
import { FlatList, RefreshControl, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { LawyerListSkeleton } from '@/components/home/HomeSkeleton';
import { LawyerCard } from '@/components/home/LawyerCard';
import { Text } from '@/components/ui/Text';
import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useOpenLawyerProfile } from '@/features/discovery/hooks/useOpenLawyerProfile';
import { useNearbyLawyersQuery } from '@/features/home/hooks/useHomeQueries';
import { useDeviceLocation } from '@/hooks/useDeviceLocation';
import type { HomeStackParamList } from '@/navigation/types';
import { useRecentLawyersStore } from '@/store/recentLawyersStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<HomeStackParamList, 'NearbyLawyers'>;

export function NearbyLawyersScreen({ navigation }: Props) {
  const theme = useTheme();
  const { location, error, refresh: refreshLocation } = useDeviceLocation();
  const nearby = useNearbyLawyersQuery(location, 40, 75);
  const addRecent = useRecentLawyersStore((s) => s.addRecent);
  const openLawyerProfile = useOpenLawyerProfile();

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader
          title="Nearby"
          subtitle={location.source === 'device' ? 'Based on your location' : 'Default city (permission limited)'}
          onBack={() => navigation.goBack()}
        />
        {error ? (
          <Text variant="caption" color="tertiary" style={{ marginBottom: theme.spacing.sm }}>
            {error}
          </Text>
        ) : null}
        {nearby.isLoading ? (
          <LawyerListSkeleton />
        ) : nearby.isError ? (
          <ErrorState
            message="Could not load nearby lawyers."
            onRetry={() => {
              void refreshLocation();
              void nearby.refetch();
            }}
          />
        ) : (
          <FlatList
            data={nearby.data?.items ?? []}
            keyExtractor={(item) => item.lawyerId}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'] }}
            refreshControl={
              <RefreshControl
                refreshing={nearby.isRefetching}
                onRefresh={async () => {
                  await refreshLocation();
                  await nearby.refetch();
                }}
                tintColor={theme.colors.accent}
              />
            }
            ListEmptyComponent={
              <EmptyState
                variant="search"
                title="Nobody nearby"
                description="Try expanding search from Home or browse Top rated."
              />
            }
            renderItem={({ item }) => (
              <LawyerCard
                lawyer={item}
                onPress={() => {
                  addRecent(item);
                  openLawyerProfile(item.lawyerId);
                }}
              />
            )}
          />
        )}
      </View>
    </Screen>
  );
}
