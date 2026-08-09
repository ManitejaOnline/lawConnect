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
import { useLawyerSearchQuery } from '@/features/home/hooks/useHomeQueries';
import type { HomeStackParamList } from '@/navigation/types';
import { useRecentLawyersStore } from '@/store/recentLawyersStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<HomeStackParamList, 'CategoryLawyers'>;

export function CategoryLawyersScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const { specializationId, title } = route.params;
  const addRecent = useRecentLawyersStore((s) => s.addRecent);
  const openLawyerProfile = useOpenLawyerProfile();

  const query = useLawyerSearchQuery({
    specializationId,
    sortBy: 'rating',
    sortDescending: true,
    pageSize: 30,
  });

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1 }}>
        <ScreenHeader title={title} subtitle="Matching lawyers" onBack={() => navigation.goBack()} />
        {query.isLoading ? (
          <LawyerListSkeleton />
        ) : query.isError ? (
          <ErrorState message="Could not load lawyers for this category." onRetry={() => query.refetch()} />
        ) : (
          <FlatList
            data={query.data?.items ?? []}
            keyExtractor={(item) => item.lawyerId}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'] }}
            refreshControl={
              <RefreshControl
                refreshing={query.isRefetching}
                onRefresh={() => query.refetch()}
                tintColor={theme.colors.accent}
              />
            }
            ListEmptyComponent={
              <EmptyState variant="search" title="No lawyers yet" description="Try another practice area." />
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
