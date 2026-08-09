import React, { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, FlatList, RefreshControl, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';

import { LawyerListSkeleton } from '@/components/home/HomeSkeleton';
import { LawyerCard } from '@/components/home/LawyerCard';
import { Chip } from '@/components/ui/Chip';
import { SearchBar } from '@/components/ui/SearchBar';
import { Text } from '@/components/ui/Text';
import { EmptyState } from '@/components/feedback/EmptyState';
import { ErrorState } from '@/components/feedback/ErrorState';
import { Screen } from '@/components/layout/Screen';
import { ScreenHeader } from '@/components/layout/ScreenHeader';
import { useInfiniteLawyerSearch } from '@/features/discovery/hooks/useDiscoveryQueries';
import type { SearchStackParamList } from '@/navigation/types';
import { useDiscoveryFiltersStore } from '@/store/discoveryFiltersStore';
import { useRecentLawyersStore } from '@/store/recentLawyersStore';
import { useTheme } from '@/theme/ThemeProvider';

type Props = NativeStackScreenProps<SearchStackParamList, 'Search'>;

const FILTER_CHIPS: { id: string; label: string; active: (applied: ReturnType<typeof useDiscoveryFiltersStore.getState>['applied']) => boolean }[] = [
  { id: 'all', label: 'All Filters', active: () => false },
  { id: 'area', label: 'Practice Area', active: (a) => Boolean(a.specializationId) },
  { id: 'location', label: 'Location', active: (a) => a.maxDistanceKm != null },
  { id: 'experience', label: 'Experience', active: (a) => a.minExperience != null },
  { id: 'fee', label: 'Fee', active: (a) => a.minFee != null || a.maxFee != null },
  { id: 'rating', label: 'Rating', active: (a) => a.minRating != null },
  { id: 'language', label: 'Language', active: (a) => Boolean(a.languageCode) },
];

export function SearchScreen({ navigation, route }: Props) {
  const theme = useTheme();
  const initialQuery = route.params?.query ?? '';
  const [draft, setDraft] = useState(initialQuery);
  const [submitted, setSubmitted] = useState(initialQuery);

  const applied = useDiscoveryFiltersStore((s) => s.applied);
  const applyDraft = useDiscoveryFiltersStore((s) => s.applyDraft);
  const replaceDraft = useDiscoveryFiltersStore((s) => s.replaceDraft);
  const activeCount = useDiscoveryFiltersStore((s) => s.activeFilterCount());
  const addRecent = useRecentLawyersStore((s) => s.addRecent);

  useEffect(() => {
    setDraft(route.params?.query ?? '');
    setSubmitted(route.params?.query ?? '');
    if (route.params?.specializationId) {
      replaceDraft({
        ...useDiscoveryFiltersStore.getState().applied,
        specializationId: route.params.specializationId,
      });
      applyDraft();
    }
  }, [route.params?.query, route.params?.specializationId, applyDraft, replaceDraft]);

  const search = useInfiniteLawyerSearch(submitted);
  const lawyers = useMemo(
    () => search.data?.pages.flatMap((page) => page.items) ?? [],
    [search.data],
  );
  const totalCount = search.data?.pages[0]?.totalCount;

  const openFilters = () => {
    replaceDraft({ ...applied });
    navigation.navigate('Filters');
  };

  return (
    <Screen edges={['top', 'left', 'right', 'bottom']} padded={false}>
      <View style={{ paddingHorizontal: theme.layout.gutter, flex: 1, gap: theme.spacing.sm }}>
        <ScreenHeader
          title={route.params?.title || 'Search'}
          subtitle={totalCount != null ? `${totalCount} lawyers` : activeCount > 0 ? `${activeCount} filters` : 'Find verified lawyers'}
        />

        <SearchBar
          value={draft}
          onChangeText={setDraft}
          placeholder="Name, practice area, city…"
          onSubmit={() => setSubmitted(draft.trim())}
          onClear={() => {
            setDraft('');
            setSubmitted('');
          }}
          onFilterPress={openFilters}
        />

        <FlatList
          horizontal
          data={FILTER_CHIPS}
          keyExtractor={(item) => item.id}
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={{ gap: theme.spacing.xs }}
          renderItem={({ item }) => (
            <Chip
              label={item.label}
              selected={item.id === 'all' ? activeCount > 0 : item.active(applied)}
              onPress={openFilters}
            />
          )}
        />

        {search.isLoading ? (
          <LawyerListSkeleton count={6} />
        ) : search.isError ? (
          <ErrorState message="Could not load lawyers." onRetry={() => search.refetch()} />
        ) : (
          <FlatList
            data={lawyers}
            keyExtractor={(item) => item.lawyerId}
            contentContainerStyle={{ gap: theme.spacing.sm, paddingBottom: theme.spacing['3xl'], flexGrow: 1 }}
            refreshControl={
              <RefreshControl
                refreshing={search.isRefetching && !search.isFetchingNextPage}
                onRefresh={() => search.refetch()}
                tintColor={theme.colors.accent}
              />
            }
            onEndReachedThreshold={0.4}
            onEndReached={() => {
              if (search.hasNextPage && !search.isFetchingNextPage) {
                void search.fetchNextPage();
              }
            }}
            ListEmptyComponent={
              <EmptyState
                variant="search"
                title="No lawyers found"
                description="Try adjusting filters or search terms."
                actionLabel="Clear filters"
                onAction={() => {
                  useDiscoveryFiltersStore.getState().resetAll();
                  setSubmitted('');
                  setDraft('');
                }}
              />
            }
            ListFooterComponent={
              <View style={{ paddingVertical: theme.spacing.md, gap: theme.spacing.sm }}>
                {search.isFetchingNextPage ? <ActivityIndicator color={theme.colors.accent} /> : null}
                {lawyers.length > 0 ? (
                  <Text variant="caption" color="tertiary" align="center">
                    Showing {lawyers.length}
                    {totalCount != null ? ` of ${totalCount}` : ''}
                  </Text>
                ) : null}
              </View>
            }
            renderItem={({ item }) => (
              <LawyerCard
                lawyer={item}
                onPress={() => {
                  addRecent(item);
                  navigation.navigate('LawyerProfile', { lawyerId: item.lawyerId });
                }}
              />
            )}
          />
        )}
      </View>
    </Screen>
  );
}
