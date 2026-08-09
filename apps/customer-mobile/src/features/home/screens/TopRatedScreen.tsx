import React from 'react';
import { FlatList, RefreshControl, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { LawyerListSkeleton } from '@/components/home/HomeSkeleton';
import { LawyerCard } from '@/components/home/LawyerCard';
import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useOpenLawyerProfile } from '@/features/discovery/hooks/useOpenLawyerProfile';
import { useTopRatedLawyersQuery } from '@/features/home/hooks/useHomeQueries';
import type { HomeStackParamList } from '@/navigation/types';
import { useRecentLawyersStore } from '@/store/recentLawyersStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<HomeStackParamList, 'TopRated'>;

export function TopRatedScreen({ navigation }: Props) {
  const theme = useTheme();
  const topRated = useTopRatedLawyersQuery(40);
  const addRecent = useRecentLawyersStore((s) => s.addRecent);
  const openLawyerProfile = useOpenLawyerProfile();

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title="Top rated" subtitle="Highest rated lawyers" onBack={() => navigation.goBack()} />
        {topRated.isLoading ? (
          <LawyerListSkeleton />
        ) : topRated.isError ? (
          <ErrorState message="Could not load top rated lawyers." onRetry={() => topRated.refetch()} />
        ) : (
          <FlatList
            data={topRated.data?.items ?? []}
            keyExtractor={(item) => item.lawyerId}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'] }}
            refreshControl={
              <RefreshControl
                refreshing={topRated.isRefetching}
                onRefresh={() => topRated.refetch()}
                tintColor={theme.colors.accent}
              />
            }
            ListEmptyComponent={
              <EmptyState variant="search" title="No ratings yet" description="Check back as more lawyers join." />
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
